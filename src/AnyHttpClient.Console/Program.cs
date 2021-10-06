using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace AnyHttpClient.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                System.Console.WriteLine("Hi!");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }

    class KeyVal
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    class SimpleDto
    {
        public List<int> Numbers { get; set; }

        public List<string> Words { get; set; }

        public decimal Val { get; set; }
        public string Name { get; set; }
    }
}
