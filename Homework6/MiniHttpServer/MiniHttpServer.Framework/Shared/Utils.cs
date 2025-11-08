using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Shared
{
    public static class Utils
    {
        public static byte[]? GetPathFile(string path)
        {
            byte[]? response = null;
            try
            {
                response = File.ReadAllBytes(path);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File Not Found");
            }
            return response;
        }
    }
}
