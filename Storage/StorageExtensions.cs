namespace Dacris.Maestro.Storage
{
    public static class StorageExtensions
    {
        public static string WithoutPrefix(this string path)
        {
            var prefixIndex = path.IndexOf(':') + 1;
            if (prefixIndex <= 1)
            {
                return path;
            }
            return path.Substring(prefixIndex, path.Length - prefixIndex);
        }

        public static void UseStoragePaths(this InteractionInputSpec inputSpec)
        {
            foreach (var input in inputSpec.Inputs[0].ValueSpec.ObjectSpecs!)
            {
                if (input.Name.ToLowerInvariant().Contains("dir") || input.Name.ToLowerInvariant().Contains("file"))
                {
                    input.WithSimpleType(ValueTypeSpec.StoragePath);
                }
            }
        }

        public static void AddStorageAbstractionInput(this InteractionInputSpec inputSpec)
        {
            var remoteSystems = (string[])["SFTP"];
            var localSystems = (string[])["Local"];
            inputSpec.Inputs.Add(new InputSpec
            {
                Name = "storageLocations",
                ValueSpec = new ValueSpec
                {
                    ValueType = ValueTypeSpec.Object,
                    ObjectSpecs = [new InputSpec
                    {
                        Name = "@prefix",
                        ValueSpec = new ValueSpec
                        {
                            ValueType = ValueTypeSpec.Object,
                            ObjectSpecs = [
                                new InputSpec("systemType")
                                {
                                    ValueSpec = new ValueSpec
                                    {
                                        ValueType = ValueTypeSpec.Enum,
                                        AcceptedValues = remoteSystems.Union(localSystems).ToArray()
                                    }
                                },
                                new InputSpec("serverHost").DependsOn("systemType", remoteSystems),
                                new InputSpec("user").DependsOn("systemType", ["SFTP"]),
                                new InputSpec("secretKey").DependsOn("systemType", remoteSystems),
                                new InputSpec("port").WithSimpleType(ValueTypeSpec.Integer).DependsOn("systemType", ["SFTP"])]
                        }
                    }]
                }
            });
            inputSpec.Inputs.Add(new InputSpec
            {
                Name = "customStorageProviders",
                ValueSpec = new ValueSpec
                {
                    ValueType = ValueTypeSpec.Object,
                    ObjectSpecs = [
                        new InputSpec("@systemType").WithSimpleType(ValueTypeSpec.String)
                    ]
                }
            });
        }
    }
}
