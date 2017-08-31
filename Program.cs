using Boissonnot.Framework.Core.WebServices.Soap.SoapWithAttachments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppApiColissimo
{
    class Program
    {
        static void Main(string[] args)
        {
            string startFilePath = @"G:\testeeee\";

            using (WebServiceClient client = new WebServiceClient())
            {
                using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
                {
                    if (OperationContext.Current.IncomingMessageProperties.ContainsKey(SwaEncoderConstants.AttachmentProperty))
                    {
                        byte[] b = (byte[])OperationContext.Current.IncomingMessageProperties[SwaEncoderConstants.AttachmentProperty];
                        using (FileStream fs = new FileStream(startFilePath +  DateTime.Now.Ticks.ToString() + ".pdf", FileMode.Create))
                        {
                            fs.Write(b, 0, b.Length);
                            fs.Flush();
                        }
                    }
                }
            }
        }
    }
}