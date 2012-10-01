// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="">
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
            var image = new Bitmap(inputFilePath);
            var scores = new List<List<int>>();
            var topScores = new List<int>();

            for (int i = 0; i < image.Width / ChunkSize; ++i)
            {
                scores.Add(new List<int>());

                for (int j = 0; j < image.Width / ChunkSize; ++j)
                {
                    if (i == j)
                    {
                        scores[i].Add(-1);
                        continue;
                    }

                    scores[i].Add(GetScore(image, i * ChunkSize, (j * ChunkSize) + 31));
                }

                topScores.Add(scores[i].IndexOf(scores[i].Max()));
            }

            // output image
            var output = new Bitmap(image.Width, image.Height, image.PixelFormat);

            var sum = scores.Select(s => s.Sum()).ToList();

            int currentChunk = sum.IndexOf(sum.Min());
            topScores[currentChunk] = -1;

            for (int i = 0; i < image.Width / ChunkSize; ++i)
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

            for (int j = 0; j < image.Height; ++j)
            {
                Color leftPixel = image.GetPixel(left, j);
                Color rightPixel = image.GetPixel(right, j);

                if (Math.Abs((leftPixel.R + leftPixel.G + leftPixel.B) - (rightPixel.R + rightPixel.G + rightPixel.B)) / 3 < 5)
                {
                    ++hits;
                }
            }

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
