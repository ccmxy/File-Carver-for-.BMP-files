/*
BMP file carver.

Author: Colleen Minor

Purpose: A method to 
carve out and validate BMP files, based on the
way they are formatted:
https://en.wikipedia.org/wiki/BMP_file_format
*/


using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace ConsoleApp9

{
    class BmpFileHeader
    {

        public int imageHeight { get; set; }
        public int imageWidth { get; set; }
        public int bitsPerPixel { get; set; }
        public int rowSize { get; set; }
        public int imageSize { get; set; }
        public int dibHeaderSize { get; set; }
        public int pixelArraySize { get; set; }
        public int minIndex { get; set; }
        public int maxIndex { get; set; }

        public BmpFileHeader(byte[] headerBytes, int startingIndex)
        {
            minIndex = startingIndex;
            imageHeight = GetImageHeight(headerBytes);
            imageWidth = GetImageWidth(headerBytes);
            bitsPerPixel = getBitsPerPixel(headerBytes);
            rowSize = getRowSize(bitsPerPixel, imageWidth);
            imageSize = GetImageDataSize(headerBytes);
            maxIndex = getMaxIndex(minIndex, imageSize);
            pixelArraySize = rowSize * imageHeight;
            dibHeaderSize = getDIBHeaderSize(headerBytes);

        }


        /* * BMP HEADER RELATED FUNCTIONS: FUNCTIONS RELATING TO THE 
      * BMP HEADER, FIRST 14 BYTES IN A BMP FILE.
        Source: https://en.wikipedia.org/wiki/BMP_file_format#Bitmap_file_header
         Offset hex	Offset dec	Size	Purpose
         00	0	2 bytes	The header field used to identify the BMP and DIB file is 0x42 0x4D in hexadecimal, same as BM in ASCII. 
         02	2	4 bytes	The size of the BMP file in bytes
         06	6	2 bytes	Reserved; actual value depends on the application that creates the image
         08	8	2 bytes	Reserved; actual value depends on the application that creates the image
         0A	10	4 bytes	The offset, i.e. starting address, of the byte where the bitmap image data (pixel array) can be found.
             */

        //Returns image size in bytes (starts at bit 2) from sent in array of bmpFile bytes
        public static int GetImageDataSize(byte[] bmpFile)
        {
            var data = new byte[4];
            data = GeneralUse.FillByteArray(bmpFile, 2, 4);
            return BitConverter.ToInt32(data, 0);
        }

        /*DIB HEADER FUNCTIONS: 
        This assumes that a bitmap header later than BITMAPCOREHEADER is being used.
        https://en.wikipedia.org/wiki/BMP_file_format#DIB_header_.28bitmap_information_header.29
        Offset (hex)	Offset (dec)	Size (bytes)	Windows BITMAPINFOHEADER[1]
        0E	14	4	the size of this header (40 bytes)
        12	18	4	the bitmap width in pixels (signed integer)
        16	22	4	the bitmap height in pixels (signed integer)
        1A	26	2	the number of color planes (must be 1)
        1C	28	2	the number of bits per pixel, which is the color depth of the image. Typical values are 1, 4, 8, 16, 24 and 32.
        1E	30	4	the compression method being used. See the next table for a list of possible values
        22	34	4	the image size. This is the size of the raw bitmap data; a dummy 0 can be given for BI_RGB bitmaps.
        26	38	4	the horizontal resolution of the image. (pixel per meter, signed integer)
        2A	42	4	the vertical resolution of the image. (pixel per meter, signed integer)
        2E	46	4	the number of colors in the color palette, or 0 to default to 2n
        32	50	4	the number of important colors used, or 0 when every color is important; generally ignored
         */

        //Returns DIB header size
        public static int getDIBHeaderSize(byte[] bmpFile)
        {
            var data = new byte[4];
            data = GeneralUse.FillByteArray(bmpFile, 14, 4);
            return BitConverter.ToInt32(data, 0);
        }

        public static int getHeaderSize(byte[] bmpFile)
        {
            var data = new byte[4];
            data = GeneralUse.FillByteArray(bmpFile, 14, 4);
            return BitConverter.ToInt32(data, 0);
        }

        //Returns row size according tho this calculation:
        // https://en.wikipedia.org/wiki/BMP_file_format#Pixel_storage
        public static int getRowSize(int bitsPerPixel, int imageWidth)
        {
            int rowSize = ((bitsPerPixel * imageWidth + 31) / 32) * 4;
            return rowSize;
        }

        public static int getBitsPerPixel(byte[] bmpFile)
        {
            var data = new byte[4];
            data = GeneralUse.FillByteArray(bmpFile, 14, 4);
            return BitConverter.ToInt32(data, 0);
        }

        //Returns image height (starts at bit 22) from sent in array of bmpFile bytes
        public static int GetImageHeight(byte[] bmpFile)
        {
            var data = new byte[4];
            data = GeneralUse.FillByteArray(bmpFile, 22, 4);
            int imageHeight = BitConverter.ToInt32(data, 0);
            return imageHeight;
        }

        //Returns image width (starts at bit 18) from sent in array of bmpFile bytes
        public static int GetImageWidth(byte[] bmpFile)
        {
            var data = new byte[4];
            data = GeneralUse.FillByteArray(bmpFile, 18, 4);
            int imageWidth = BitConverter.ToInt32(data, 0);
            return imageWidth;
        }

        public static int getMaxIndex(int startingIndex, int imageSize)
        {
            return startingIndex + imageSize;
        }


        /*FUNCTIONS RELATING TO VALIDITY OF FILE*/
        public bool isValid()
        {
            if (!(isHeaderSizeValid())) { return false; }
            if (!(isPixelArraySizeValid())) { return false; }
            return true;
        }

        //Returns true if the dib header size is either 40, 52, 56, 128, or 124, false if otherwise
        private bool isHeaderSizeValid()
        {
            switch (dibHeaderSize)
            {
                case 40:
                    return true;
                case 52:
                    return true;
                case 56:
                    return true;
                case 128:
                    return true;
                case 124:
                    return true;
                default:
                    return false;
            }
        }

        /*Other vaidation: Row size should = (((bits per pixel * image width) + 31) / 32) * 4
      * and PixelArraySize should = RowSize * ImageHeight
    * source: https://en.wikipedia.org/wiki/BMP_file_format#Pixel_array_.28bitmap_data.29
    * */
        private bool isPixelArraySizeValid()
        {
            if (pixelArraySize == (rowSize * imageHeight)) { return true; }
            else { return false; }
        }

    }

    class WriteAndHashData
    {
        public static void Driver(byte[] data, List<BmpFileHeader> fileHeaderList, string fileName)
        {
            int idx;
            byte[] buffer;
            bool fileStartsWithBMP;
            int lastFileHeaderIdx = fileHeaderList.Count - 1;

            //Create folder in path that file is in
            String directoryName = fileName + "_Output";
            Directory.CreateDirectory(directoryName); //Create directory

            //Check if file starts with bmp or other file
            fileStartsWithBMP = false;
            foreach (var fileHeader in fileHeaderList)
            {
                if (fileHeader.minIndex == 0)
                {
                    fileStartsWithBMP = true;
                }
            }
            if (!fileStartsWithBMP) //case data before first bmp file
            {
                buffer = GeneralUse.FillByteArray(data, 0, fileHeaderList[0].minIndex);
                makeFile(directoryName, buffer, 0, false);
            }

            //Loop through each bmp file and make al files
            for (idx = 0; idx < fileHeaderList.Count; idx++)
            {
                buffer = GeneralUse.FillByteArray(data, fileHeaderList[idx].minIndex, fileHeaderList[idx].imageSize);
                makeFile(directoryName, buffer, fileHeaderList[idx].minIndex, true);
                if (idx == lastFileHeaderIdx) { break; } //If end of fileHeaderList reached
                if (fileHeaderList[idx].maxIndex + 1 < fileHeaderList[idx + 1].minIndex)
                {
                    buffer = GeneralUse.FillByteArray(data, fileHeaderList[idx].maxIndex + 1, fileHeaderList[idx + 1].minIndex - 1);
                    makeFile(directoryName, buffer, fileHeaderList[idx].maxIndex + 1, false);
                }
            } //End for loop

            if (fileHeaderList[lastFileHeaderIdx].maxIndex < data.Length - 1) //In case there is data after last bmp file
            {
                int numBytes = data.Length - 1 - (fileHeaderList[lastFileHeaderIdx].maxIndex);
                buffer = GeneralUse.FillByteArray(data, fileHeaderList[lastFileHeaderIdx].maxIndex, numBytes);
                makeFile(directoryName, buffer, fileHeaderList[lastFileHeaderIdx].maxIndex + 1, false);
            }

        }



        private static void makeFile(String directoryName, byte[] buffer, int offset, bool isBMPFile)

        {

            if (!isBMPFile)
            {
                Console.WriteLine("\n\nOther file detected at " + offset + " of size " + buffer.Length + " bytes.");
                hashIt(buffer);

                using (FileStream otherFile = File.Create(directoryName + @"\" + offset + ".other"))
                {
                    otherFile.Write(buffer, 0, buffer.Length);
                    otherFile.Close();
                }
                Console.WriteLine("View this file at " + directoryName + @"\" + offset + ".other");
            }
            else //if is BMP file
            {
                Console.WriteLine("\n\nBMP file detected at " + offset + " of size " + buffer.Length + " bytes.");
                hashIt(buffer);

                using (FileStream bmpFile = File.Create(directoryName + @"\" + offset + ".bmp"))
                {
                    bmpFile.Write(buffer, 0, buffer.Length);
                    bmpFile.Close();
                }

                Console.WriteLine("View this file at " + directoryName + @"\" + offset + ".other");
            }
        }


        /*Source for all the hash functions: https://msdn.microsoft.com/en-us/library/s02tk69a(v=vs.110).aspx */
        public static void hashIt(byte[] buffer)
        {
            string source = BitConverter.ToString(buffer);
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = GetMd5Hash(md5Hash, source);

                Console.WriteLine("The MD5 hash of this source is: " + hash + ".");

                if (!VerifyMd5Hash(md5Hash, source, hash))
                {
                    Console.WriteLine("Hash mismatch");
                }

            }

        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        static bool VerifyMd5Hash(MD5 md5Hash, string input, string hash)
        {
            // Hash the input.
            string hashOfInput = GetMd5Hash(md5Hash, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }

    class GeneralUse
    {
        //GENERAL USE FUNCTIONS
        public static byte[] loadByteArray(string fileName)
        {
            long length; //Length of file

            FileStream fs = new FileStream(fileName, FileMode.Open);
            length = new FileInfo(fileName).Length;
            fs.Position = 0;
            var data = new byte[length];
            int numRead = 0;
            do
            {
                numRead += fs.Read(data, numRead, (int)length - numRead);
            } while (numRead != length && fs.Position < fs.Length);
            fs.Close();

            return data;
        }
        public static long GetFileSize(string fileName)
        {
            long length = new FileInfo(fileName).Length; //Length of file
            return length;
        }
        public static byte[] FillByteArray(byte[] inputByteArray, int inputStartIdx, long numBytes)
        {
            int idx; //loop counter and int to hold the index of the output bytes
            var outputByteArray = new byte[numBytes]; //Array of bytes to return

            for (idx = 0; idx < numBytes; idx++)
            { //Fill output array with bytes
                outputByteArray[idx] = inputByteArray[inputStartIdx + idx];
            }

            return outputByteArray;

        }


    }


    class bmpListFunctions
    {
        /*Returns a list of byte[] arrays that are the length of the 
       bitmap file header + the DIB header */
        public static List<byte[]> getPossibleHeaderBytesList(byte[] data, List<int> headerIndexList)
        {
            List<byte[]> possibleHeaderBytesList = new List<byte[]>();
            int dibHeaderSize;
            byte[] header;
            for (int i = 0; i < headerIndexList.Count; i++)
            {
                header = GeneralUse.FillByteArray(data, headerIndexList[i], 18); //Load with bitmap header + 4 to grab dib header size
                dibHeaderSize = BmpFileHeader.getDIBHeaderSize(header);
                header = GeneralUse.FillByteArray(data, headerIndexList[i], 14 + dibHeaderSize); //fill byte array with both bmp and dib header
                possibleHeaderBytesList.Add(header);
            }
            return possibleHeaderBytesList;
        }

        //Returns a list of possible header indexes (bytes that spell BM) (might have to make hex first not sure)
        //Runs in Theta(dataLength)
        public static List<int> getHeaderIndexes(byte[] data)
        {
            int idx;
            int dataLength = data.Length;
            List<int> headerIndexes = new List<int>();
            string hex = BitConverter.ToString(data);

            var byteMap = new byte[dataLength];
            BitConverter.ToInt32(data, 0);


            for (idx = 0; idx < data.Length; idx++)
            {
                if (data[idx] == (0x42 & 0xFF) && data[idx + 1] == (0x4D & 0xFF))
                {
                    headerIndexes.Add(idx);
                }
            }

            return headerIndexes;
        }

        public static List<BmpFileHeader> createSortedList(List<BmpFileHeader> unsortedHeap)
        {
            //Simple insertion sort
            int j;
            for (int i = 1; i < unsortedHeap.Count; i++)
            {
                j = i;
                while (j > 0 && unsortedHeap[j - 1].maxIndex > unsortedHeap[j].maxIndex)
                {
                    BmpFileHeader tmp = unsortedHeap[j - 1];
                    unsortedHeap[j - 1] = unsortedHeap[j];
                    unsortedHeap[j] = tmp;

                }

            }
            return unsortedHeap;
        }
    }

    class Carver
    {

        public static void Main(string[] args)
        {
            Console.WriteLine("Please enter the name of the input file: ");
            string fileName = Console.ReadLine();
            if (!isFileValid(fileName))
            {
                Console.WriteLine("Oh no! " + fileName + " is not a valid filename. Press enter to exit.");
                Console.ReadLine();
                return;
            }
            else
            {
                driver(fileName);
            }

        }
        private static bool isFileValid(string fileName)
        {
            if (File.Exists(fileName)) { return true; }
            else
            {
                Console.WriteLine("{0} does not exist!", fileName);
                return false;
            }

        }
        private static void driver(string fileName)
        {
            long length = GeneralUse.GetFileSize(fileName);
            var data = new byte[length];
            data = GeneralUse.loadByteArray(fileName);

            //Create a list of file headers 

            List<int> possibleHeaderIndexes = bmpListFunctions.getHeaderIndexes(data);
            List<byte[]> possibleHeaderBytes = bmpListFunctions.getPossibleHeaderBytesList(data, possibleHeaderIndexes);
            List<BmpFileHeader> fileHeaderHeap = new List<BmpFileHeader>();

            for (int idx = 0; idx < possibleHeaderBytes.Count; idx++)
            {
                BmpFileHeader fileHeader = new BmpFileHeader(possibleHeaderBytes[idx], possibleHeaderIndexes[idx]);
                //BmpFileAccessors fileHeader = BmpFileHeaderList.setFileHeader(possibleHeaderBytes[idx], possibleHeaderIndexes[idx]);

                if (fileHeader.isValid())
                {
                    fileHeaderHeap.Add(fileHeader);

                }
            }
            fileHeaderHeap = bmpListFunctions.createSortedList(fileHeaderHeap);

            WriteAndHashData.Driver(data, fileHeaderHeap, fileName);
            Console.WriteLine("Press enter to exit");
            Console.Read();

        }
    }

}