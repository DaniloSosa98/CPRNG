using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace PrimeGen {
    class Program {
        /// @author Danilo Sosa, CS @ RIT
        /// <summary>
        /// The Main function will read the bits and the optional
        /// count if entered. It will proceed to validate if the
        /// arguments are valid and create a Prime object and call
        /// its respective methods.
        /// Usage: dotnet run <bits> <count=1>
        /// - bits - the number of bits of the prime number, this must be a
        ///   multiple of 8, and at least 32 bits.
        /// - count - the number of prime numbers to generate, defaults to 1
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args) {
            if (args.Length== 1 && args[0].Equals("help")) {
                Console.WriteLine("Usage: dotnet run <bits> <count=1>" +
                                  "\n- bits - the number of bits of the prime number, this must be a" +
                                  "\n  multiple of 8, and at least 32 bits." +
                                  "\n- count - the number of prime numbers to generate, defaults to 1");
                Environment.Exit(1);
            }
            int bits = 0, count = 0;
            //If there is only one argument, the count is default
            //to 1.
            if (args.Length == 1) {
                count = 1;
            
            //If there are 2 arguments validate and assign them
            //or print error and exit
            }else if (args.Length == 2) {
                count = intParse(args[1]);
                if (count<1) {
                    Console.WriteLine("Usage: dotnet run <bits> <count=1>" +
                                      "\n Error: {0} is less than 1", count);
                    Environment.Exit(1);
                }
                
            //If no arguments are entered print error and exit    
            }else {
                Console.WriteLine("Usage: dotnet run <bits> <count=1>" +
                                  "\n Error: Incorrect amount of arguments");
                Environment.Exit(1);
            }
            bits = intParse(args[0]);
            //Validate bits entered
            var by = checkBits(bits);
            //Create stopwatch to know run time
            Stopwatch st = new Stopwatch();
            //Create Prime object assigning entered arguments
            Prime pr = new Prime(by, count);
            //Call Prime class methods
            Console.WriteLine("BitLength: {0} bits", bits);
            st.Start();
            pr.getPrimes();
            st.Stop();
            //Format and print runtime
            TimeSpan ts = st.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds);
            Console.WriteLine("Time to Generate: " + elapsedTime);
        }
        
        /// <summary>
        /// Function that validates if the arguments are numbers
        /// if false print error message and exit
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int intParse(string b) {
            int x;
            if (!Int32.TryParse(b, out x)) {
                Console.WriteLine("Usage: dotnet run <bits> <count=1>" +
                                  "\n Error: '{0}' is not a number", b);
                Environment.Exit(1);
            }
            return x;
        }
        
        /// <summary>
        /// Function that validates if the entered bit
        /// are divisible by 8 and at least 32.
        /// If not, print error and exit
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private static byte checkBits(int b) {
            if (b%8!=0 || b<32) {
                Console.WriteLine("Error: {0} is not divisible by 8 or at least 32", b);
                Environment.Exit(1);
            }

            return (byte) (b/8);
        }
        
    }

    /// <summary>
    /// Prime class that contains the bytes and count.
    /// With methods to get the prime numbers
    /// </summary>
    public class Prime {
        public byte by{ get; private set; }
        public int count { get; private set; }
        
        /// <summary>
        /// Prime constructor receives bytes and count
        /// </summary>
        /// <param name="by"></param>
        /// <param name="count"></param>
        public Prime(byte @by, int count) {
            this.@by = @by;
            this.count = count;
        }
        
        /// <summary>
        /// Function with a Parallel loop to get the Prime
        /// numbers
        /// </summary>
        public void getPrimes() {
            int index = 0;
            //Parallel For loop that runs until we get the 
            //requested amount of prime numbers
            Parallel.For(0, Int32.MaxValue, (i, state) => {
                if (index < count) {
                    BigInteger bi = this.genRand();
                    if (bi.IsProbablyPrime()) {
                        Interlocked.Increment(ref index);
                        printResult(index, bi);
                    }
                }
                else {
                    state.Stop();
                }
            });

        }
        
        public  void printResult(int index, BigInteger bi) {
            if (index > count) {
                return;
            }
            if (index!=1) {
                Console.WriteLine("\n{0}: {1}", index, bi);
            }else {
                Console.WriteLine("{0}: {1}", index, bi);
            }
        }
    }

    /// <summary>
    /// Class for the Extension Methods
    /// </summary>
    public static class ExtensionMethods {
        private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        
        /// <summary>
        /// Provided Extension method that checks if a BigInteger
        /// is prime
        /// </summary>
        /// <param name="value"></param>
        /// <param name="witnesses"></param>
        /// <returns></returns>
        public static Boolean IsProbablyPrime(this BigInteger value, int witnesses = 10) {
            if (value <= 1) return false;
            if (witnesses <= 0) witnesses = 10;
            BigInteger d = value - 1;
            int s = 0;
            while (d % 2 == 0) {
                d /= 2;
                s += 1;
            }
            Byte[] bytes = new Byte[value.ToByteArray().LongLength];
            BigInteger a;
            for (int i = 0; i < witnesses; i++) {
                do {
                    var Gen = new Random();
                    Gen.NextBytes(bytes);
                    a = new BigInteger(bytes);
                } while (a < 2 || a >= value - 2);
                BigInteger x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == value - 1) continue;
                for (int r = 1; r < s; r++) {
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == 1) return false;
                    if (x == value - 1) break;
                }
                if (x != value - 1) return false;
            }
            return true;
        }
        
        /// <summary>
        /// Function that generates a random
        /// set of bytes that are later used
        /// in the BigInteger class
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static BigInteger genRand(this Prime val) {
            //Byte set with of size 'by'
            byte[] set = new byte[val.by];
            //Generate the random set of bytes
            rng.GetBytes(set);
            //Send bytes to BigInteger class
            BigInteger value = new BigInteger(set);
            //Return BigInteger
            return value;
        }
    }
    
    
}
