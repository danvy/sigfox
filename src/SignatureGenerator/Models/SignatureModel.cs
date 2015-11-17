using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignatureGenerator.Models
{
    public class SignatureModel
    {
/*
        public static TokenProvider CreateTokenProviderForSender(string senderKeyName, string senderKey, string serviceNamespace, string hubName, string publisherName, TimeSpan tokenTimeToLive)
        {
            return TokenProvider.CreateSharedAccessSignatureTokenProvider(CreateForSender(senderKeyName, senderKey, serviceNamespace, hubName, publisherName, tokenTimeToLive));
        }

        public static string CreateForSender(string senderKeyName, string senderKey, string serviceNamespace, string hubName, string publisherName, TimeSpan tokenTimeToLive)
        {
            var serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", serviceNamespace, String.Format("{0}/publishers/{1}", hubName, publisherName))
                .ToString()
                .Trim('/');
            return SharedAccessSignatureTokenProvider.GetSharedAccessSignature(senderKeyName, senderKey, serviceUri, tokenTimeToLive);
        }

        public static string CreateForHttpSender(string senderKeyName, string senderKey, string serviceNamespace, string hubName, string publisherName, TimeSpan tokenTimeToLive)
        {
            var serviceUri = ServiceBusEnvironment.CreateServiceUri("https", serviceNamespace, String.Format("{0}/publishers/{1}/messages", hubName, publisherName))
                .ToString()
                .Trim('/');
            return SharedAccessSignatureTokenProvider.GetSharedAccessSignature(senderKeyName, senderKey, serviceUri, tokenTimeToLive);
        }
    }
    */
}
