using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JShibo.Compression.Tests
{
    public class ShiboCompressionTests
    {

        public static void RunTests()
        {
            byte[] bytes = ShiboRandom.GetRandomBytes(1024 * 1024);
            //压缩
            byte[] cbytes = ShiboCompression.Compress(bytes);
            //解压
            byte[] dbytes = ShiboDecompress.Decode(cbytes);
            //比较是否相同
            bool b = ShiboComparer.Compare(bytes, dbytes);
            Console.WriteLine("结果一致：" + b);
            Console.ReadLine();


        }

        /// <summary>
        /// 进行压力情况下的准确性测试
        /// </summary>
        public static void RunStress()
        {
            for (int i = 0; i < 100000; i++)
            {

            }
        }



    }
}
