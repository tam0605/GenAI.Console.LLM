using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace DemoUnitTest_ConsoleApp
{
    public class Calculator
    {
        /// <summary>
        /// Phương thức cộng hai số nguyên
        /// </summary>
        public int Add(int a, int b)
        {
            return a + b;
        }

        /// <summary>
        /// Phương thức trừ hai số nguyên
        /// </summary>
        public int Subtract(int a, int b)
        {
            return a - b;
        }

        /// <summary>
        /// Phương thức nhân hai số nguyên
        /// </summary>
        public int Multiply(int a, int b)
        {
            return a * b;
        }

        /// <summary>
        /// Phương thức chia hai số nguyên, có ném ra ngoại lệ nếu chia cho 0
        /// </summary>
        public double Divide(int a, int b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException("Không thể chia cho số 0.");
            }
            return (double)a / b;
        }
    }
}
