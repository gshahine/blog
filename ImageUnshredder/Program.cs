// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="http://gshahine.com">
//   Guy Shahine. All rights reserved
// </copyright>
// <summary>
//   An image Unshredder program
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ImageUnshredder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Image unshredder program
    /// </summary>
    public class Program
    {
        /// <summary>
        ///  The predefined chunk size is 32 pixels
        /// </summary>
        private const int ChunkSize = 32;

        /// <summary>
        /// Used to lock reads to image when computing score
        /// </summary>
        private static object scoreLockObject = new object();

        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args">command line args (not used)</param>
        private static void Main(string[] args)
        {
            var sp = Stopwatch.StartNew();

            Unshred("TokyoPanoramaShredded.png", "output.png");

            sp.Stop();

            Console.WriteLine("Image Unshredded in {0}ms", sp.ElapsedMilliseconds);
            Console.ReadLine();
        }

        /// <summary>
        /// The method that unshreds an input image and stores the result in an output image.
        /// </summary>
        /// <param name="inputFilePath">The path to the image to unshred</param>
        /// <param name="outputFilePath">The path to the result image</param>
        private static void Unshred(string inputFilePath, string outputFilePath)
        {
            var sp = Stopwatch.StartNew();
            var image = new Bitmap("TokyoPanoramaShredded.png");
            var topScores = new List<int>();

            int chunks = image.Width / ChunkSize;
            var scoreTasks = new Task<List<int>>[chunks];

            for (int i = 0; i < chunks; ++i)
            {
                int captured = i;
                scoreTasks[i] = Task.Factory.StartNew<List<int>>(() =>
                {
                    var s = new List<int>();

                    for (int j = 0; j < chunks; ++j)
                    {
                        if (captured == j)
                        {
                            s.Add(-1);
                            continue;
                        }

                        s.Add(GetScore(image, captured * ChunkSize, (j * ChunkSize) + 31));
                    }

                    return s;
                });
            }

            Task.WaitAll(scoreTasks);

            for (int i = 0; i < chunks; ++i)
            {
                topScores.Add(scoreTasks[i].Result.IndexOf(scoreTasks[i].Result.Max()));
            }

            // output image
            var output = new Bitmap(image.Width, image.Height, image.PixelFormat);

            var sum = scoreTasks.Select(s => s.Result.Sum()).ToList();

            int currentChunk = sum.IndexOf(sum.Min());
            topScores[currentChunk] = -1;

            for (int i = 0; i < chunks; ++i)
            {
                CopyChunk(image, output, currentChunk, i);
                currentChunk = topScores.IndexOf(currentChunk);
            }

            output.Save(outputFilePath);
        }

        /// <summary>
        /// A naive approach to check if the pixels in two columns are close to each other in color
        /// </summary>
        /// <param name="image">Reference to the image</param>
        /// <param name="right">right column index</param>
        /// <param name="left">left column index</param>
        /// <returns>A score for how close the two columns are to each other</returns>
        private static int GetScore(Bitmap image, int right, int left)
        {
            int hits = 0;

            Monitor.Enter(scoreLockObject);
            for (int j = 0; j < image.Height; ++j)
            {
                Color leftPixel = image.GetPixel(left, j);
                Color rightPixel = image.GetPixel(right, j);

                if (Math.Abs((leftPixel.R + leftPixel.G + leftPixel.B) - (rightPixel.R + rightPixel.G + rightPixel.B)) / 3 < 5)
                {
                    ++hits;
                }
            }
            Monitor.Exit(scoreLockObject);

            return hits;
        }

        /// <summary>
        /// Copies a chunk of pixels data in a buffer
        /// </summary>
        /// <param name="source">The source image</param>
        /// <param name="destination">The destination image</param>
        /// <param name="sourceChunk">The source chunk start indes</param>
        /// <param name="destinationChunk">The destination chunk start index</param>
        private static void CopyChunk(Bitmap source, Bitmap destination, int sourceChunk, int destinationChunk)
        {
            for (int j = 0; j < ChunkSize; ++j)
            {
                for (int i = 0; i < destination.Height; ++i)
                {
                    destination.SetPixel((destinationChunk * ChunkSize) + j, i, source.GetPixel((sourceChunk * ChunkSize) + j, i));
                }
            }
        }
    }
}
