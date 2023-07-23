/*
 * Date: 2012-05-26
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Interface for handling <see cref="Id3v2Frame"/> encryption/decryption.
    /// </summary>
    public interface IId3v2FrameCryptor
    {
        /// <summary>
        /// Encrypts the specified data.
        /// </summary>
        /// <param name="encryptionType">Type of the encryption.</param>
        /// <param name="data">The data.</param>
        /// <returns>
        /// The encrypted data.
        /// </returns>
        byte[] Encrypt(byte encryptionType,  byte[] data);

        /// <summary>
        /// Decrypts the specified data.
        /// </summary>
        /// <param name="encryptionType">Type of the encryption.</param>
        /// <param name="data">The data.</param>
        /// <param name="dataSize">Size of the data.</param>
        /// <returns>
        /// The decrypted data.
        /// </returns>
        byte[] Decrypt(byte encryptionType, byte[] data, int dataSize);
    }
}
