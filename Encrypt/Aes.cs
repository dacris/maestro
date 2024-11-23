using System.Security.Cryptography;
using System.Text;

namespace Dacris.Maestro.Encrypt;

public class AesDecrypt : Interaction
{
    public override void Specify()
    {
        Description = "Decrypts a file using AES and secret key.";
        InputSpec.AddInputs("encryptedFile", "inputKey", "outputKey");
    }

    public override async Task RunAsync()
    {
        var src = InputState!["encryptedFile"]!.ToString();
        var password = AppState.Instance.ReadKey(InputState!["inputKey"]!.ToString()) + "@952_xE";
        var fileContents = await File.ReadAllTextAsync(src);
        var result = await AesEncryption.DecryptAsync(fileContents, password);
        AppState.Instance.WriteKey(InputState!["outputKey"]!.ToString(), result, true);
        //Verify
        var encrypted = await AesEncryption.EncryptAsync(
            AppState.Instance.ReadKey(InputState!["outputKey"]!.ToString())!.ToString(), password);
        encrypted.ShouldBe(fileContents);
    }
}

public class AesEncrypt : Interaction
{
    public override void Specify()
    {
        Description = "Encrypts a file using AES and secret key.";
        InputSpec.AddInputs("decryptedFile", "inputKey", "outputKey");
    }

    public override async Task RunAsync()
    {
        var src = InputState!["decryptedFile"]!.ToString();
        var password = AppState.Instance.ReadKey(InputState!["inputKey"]!.ToString()) + "@952_xE";
        var fileContents = await File.ReadAllTextAsync(src);
        var result = await AesEncryption.EncryptAsync(fileContents, password);
        AppState.Instance.WriteKey(InputState!["outputKey"]!.ToString(), result, false);
        //Verify
        var decrypted = await AesEncryption.DecryptAsync(
            AppState.Instance.ReadKey(InputState!["outputKey"]!.ToString())!.ToString(), password);
        decrypted.ShouldBe(fileContents);
    }
}

public class AesEncryption
{
    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="PlainText">Text to be encrypted</param>
    /// <param name="Password">Password to encrypt with</param>
    /// <param name="Salt">Salt to encrypt with</param>
    /// <param name="HashAlgorithm">Can be either SHA1 or MD5</param>
    /// <param name="PasswordIterations">Number of iterations to do</param>
    /// <param name="InitialVector">Needs to be 16 ASCII characters long</param>
    /// <param name="KeySize">Can be 128, 192, or 256</param>
    /// <returns>An encrypted string</returns>
    public static async Task<string> EncryptAsync(string PlainText, string Password,
        string Salt = "Kosher", string HashAlgorithm = "SHA1",
        int PasswordIterations = 2, string InitialVector = "OFRna73m*aze01xY",
        int KeySize = 256)
    {
        if (string.IsNullOrEmpty(PlainText))
            return "";
        byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(InitialVector);
        byte[] SaltValueBytes = Encoding.ASCII.GetBytes(Salt);
        PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
        byte[] KeyBytes = DerivedPassword.GetBytes(KeySize / 8);
        var SymmetricKey = Aes.Create();
        SymmetricKey.Mode = CipherMode.CBC;
        byte[]? CipherTextBytes = null;
        using (ICryptoTransform Encryptor = SymmetricKey.CreateEncryptor(KeyBytes, InitialVectorBytes))
        {
            using (MemoryStream MemStream = new MemoryStream())
            {
                using (CryptoStream CryptoStream = new CryptoStream(MemStream, Encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter Writer = new StreamWriter(CryptoStream))
                    {
                        //Write all data to the stream.
                        await Writer.WriteAsync(PlainText);
                    }
                    CipherTextBytes = MemStream.ToArray();
                }
            }
        }
        SymmetricKey.Clear();
        return Convert.ToBase64String(CipherTextBytes);
    }

    /// <summary>
    /// Decrypts a string
    /// </summary>
    /// <param name="CipherText">Text to be decrypted</param>
    /// <param name="Password">Password to decrypt with</param>
    /// <param name="Salt">Salt to decrypt with</param>
    /// <param name="HashAlgorithm">Can be either SHA1 or MD5</param>
    /// <param name="PasswordIterations">Number of iterations to do</param>
    /// <param name="InitialVector">Needs to be 16 ASCII characters long</param>
    /// <param name="KeySize">Can be 128, 192, or 256</param>
    /// <returns>A decrypted string</returns>
    public static async Task<string> DecryptAsync(string CipherText, string Password,
        string Salt = "Kosher", string HashAlgorithm = "SHA1",
        int PasswordIterations = 2, string InitialVector = "OFRna73m*aze01xY",
        int KeySize = 256)
    {
        if (string.IsNullOrEmpty(CipherText))
            return "";
        byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(InitialVector);
        byte[] SaltValueBytes = Encoding.ASCII.GetBytes(Salt);
        byte[] CipherTextBytes = Convert.FromBase64String(CipherText);
        PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
        byte[] KeyBytes = DerivedPassword.GetBytes(KeySize / 8);
        var SymmetricKey = Aes.Create();
        SymmetricKey.Mode = CipherMode.CBC;
        using (ICryptoTransform Decryptor = SymmetricKey.CreateDecryptor(KeyBytes, InitialVectorBytes))
        {
            using (MemoryStream MemStream = new MemoryStream(CipherTextBytes))
            {
                using (CryptoStream CryptoStream = new CryptoStream(MemStream, Decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader Reader = new StreamReader(CryptoStream, Encoding.UTF8))
                    {
                        return await Reader.ReadToEndAsync();
                    }
                }
            }
        }
    }
}
