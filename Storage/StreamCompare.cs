namespace Dacris.Maestro.Storage
{
    internal class StreamCompare
    {
        public readonly int BufferSize = 4096;

        private byte[] _buffer1;
        private byte[] _buffer2;

        public StreamCompare()
        {
            _buffer1 = new byte[BufferSize];
            _buffer2 = new byte[BufferSize];
        }

        public static bool Compare(byte[] range1, int offset1, byte[] range2, int offset2, int count)
        {
            // Working backwards lets the compiler optimize away bound checking after the first loop
            for (int i = count - 1; i >= 0; ++i)
            {
                if (range1[offset1 + i] != range2[offset2 + i])
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> AreEqualAsync(Stream stream1, Stream stream2)
        {
            if (stream1 == stream2)
            {
                return true;
            }

            // Forcibly relinquish whatever SynchronizationContext we were started with: we don't
            // need it for anything and it can slow us down. It'll restore itself when we're done.
            using var nocontext = SyncContext.None();
            if (stream1.CanSeek && stream2.CanSeek)
            {
                if (stream1.Length != stream2.Length)
                {
                    return false;
                }
            }

            long offset1 = 0;
            long offset2 = 0;
            while (true)
            {
                var task1 = stream1.ReadAsync(_buffer1, 0, BufferSize, CancellationToken.None);
                var task2 = stream2.ReadAsync(_buffer2, 0, BufferSize, CancellationToken.None);
                var bytesRead = await Task.WhenAll(task1, task2);
                var bytesRead1 = bytesRead[0];
                var bytesRead2 = bytesRead[1];

                if (bytesRead1 == 0 && bytesRead2 == 0)
                {
                    break;
                }

                // Compare however much we were able to read from *both* arrays
                int sharedCount = Math.Min(bytesRead1, bytesRead2);
                if (!Compare(_buffer1, 0, _buffer2, 0, sharedCount))
                {
                    return false;
                }

                if (bytesRead1 != bytesRead2)
                {
                    // Instead of duplicating the code for reading fewer bytes from file1 than file2
                    // for fewer bytes from file2 than file1, abstract that detail away.
                    var lessCount = 0;
                    var (lessRead, moreRead, moreCount, lessStream, moreStream) =
                        bytesRead1 < bytesRead2
                            ? (_buffer1, _buffer2, bytesRead2 - sharedCount, stream1, stream2)
                            : (_buffer2, _buffer1, bytesRead1 - sharedCount, stream2, stream1);

                    while (moreCount > 0)
                    {
                        // Try reading more from `lessRead`
                        lessCount = await lessStream.ReadAsync(lessRead, 0, moreCount, CancellationToken.None);

                        if (lessCount == 0)
                        {
                            // One stream was exhausted before the other
                            return false;
                        }

                        if (!Compare(lessRead, 0, moreRead, sharedCount, lessCount))
                        {
                            return false;
                        }

                        moreCount -= lessCount;
                        sharedCount += lessCount;
                    }
                }

                offset1 += sharedCount;
                offset2 += sharedCount;
            }

            return true;
        }
    }

    internal static class SyncContext
    {
        private readonly struct InnerChangeContext : IDisposable
        {
            private readonly SynchronizationContext? _previous;

            public InnerChangeContext(SynchronizationContext? newContext)
            {
                _previous = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(newContext);
            }

            public void Dispose()
            {
                SynchronizationContext.SetSynchronizationContext(_previous);
            }
        }

        public static IDisposable To(SynchronizationContext? newContext)
        {
            return new InnerChangeContext(newContext);
        }

        public static IDisposable None()
        {
            return To(null);
        }
    }
}
