using System;

using AudioVideoLib.Tags;

namespace AudioVideoLibExamples
{
    /// <summary>
    /// 
    /// </summary>
    public class Id3v2FrameCryption : IId3v2FrameCryptor
    {
        public const byte SecretEncryptionType = 0xBD;

        private const byte SecretByte = 0xEE;

        /// <summary>
        /// Encrypts the specified data.
        /// </summary>
        /// <param name="encryptionType">Type of the encryption.</param>
        /// <param name="data">The data.</param>
        /// <returns>
        /// The encrypted data.
        /// </returns>
        public byte[] Encrypt(byte encryptionType, byte[] data)
        {
            switch (encryptionType)
            {
                case SecretEncryptionType:
                    return EncryptDataMethod1(data);
            }
            return data;
        }

        /// <summary>
        /// Decrypts the specified data.
        /// </summary>
        /// <param name="encryptionType">Type of the encryption.</param>
        /// <param name="data">The data.</param>
        /// <param name="dataSize">Size of the data.</param>
        /// <returns>
        /// The decrypted data.
        /// </returns>
        public byte[] Decrypt(byte encryptionType, byte[] data, int dataSize)
        {
            switch (encryptionType)
            {
                case SecretEncryptionType:
                    return DecryptDataMethod1(data);
            }
            return null;
        }

        private static byte[] EncryptDataMethod1(byte[] data)
        {
            byte[] buffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length);
            for (int i = 0; i < data.Length; i++)
                buffer[i] = (byte)(data[i] ^ SecretByte);

            return buffer;
        }

        private static byte[] DecryptDataMethod1(byte[] data)
        {
            byte[] buffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length);
            for (int i = 0; i < data.Length; i++)
                buffer[i] = (byte)(data[i] ^ SecretByte);

            return buffer;
        }
    }
}
