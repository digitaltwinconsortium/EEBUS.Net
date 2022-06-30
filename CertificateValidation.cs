
using System.Security.Cryptography.X509Certificates;

namespace EEBUS
{
    public class CertificateValidation
    {
        public bool ValidateCertificate(X509Certificate2 clientCertificate)
        {
            // TODO: Always accept for now
            return true;
        }
    }
}