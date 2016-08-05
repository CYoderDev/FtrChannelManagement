using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.Security
{
    internal class DecryptTransform : ICryptoTransformer
    {
        public byte[] IV { get { return this._iv; } set { this._iv = value; } }
        private byte[] _iv;

        public byte[] EncryptionKey { get { return this._encryptionKey; } set { this._encryptionKey = value; } }
        private byte[] _encryptionKey;

        public ICryptoTransform CreateCypher(byte[] bytesKey, Algorithm algorithmID)
        {
            switch (algorithmID)
            {
                case Algorithm.Rijndael:
                    using (Rijndael rij = new RijndaelManaged(){ Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = this._iv ?? rij.IV;
                        return rij.CreateDecryptor(bytesKey, this._iv);
                    }
                case Algorithm.TripleDes:
                    using (TripleDES des3 = new TripleDESCryptoServiceProvider() { Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = this._iv ?? des3.IV;
                        return des3.CreateDecryptor(bytesKey, this._iv);
                    }
                case Algorithm.Des:
                    using (DES des = new DESCryptoServiceProvider() { Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = this._iv ?? des.IV;
                        return des.CreateDecryptor(this._encryptionKey, this._iv);
                    }
                case Algorithm.Rc2:
                    using (RC2 rc2 = new RC2CryptoServiceProvider() { Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = this._iv ?? rc2.IV;
                        return rc2.CreateDecryptor(bytesKey, this._iv);
                    }
                default:
                    throw new CryptographicException(string.Format("Algorithm {0} does not exist", algorithmID));
            }
        }

        internal void SetIV(byte[] NewIV)
        {
            this._iv = NewIV;
        }
    }
}
