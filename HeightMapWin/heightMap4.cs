using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using HeightMapWin;

namespace HeightMap4
{
    class Location
    {
        public int x;
        public int y;
        public string key()
        {
            return x.ToString() + "_" + y.ToString();
        }
    }

    class GroupFloodData
    {
        public Dictionary<string, Location> groupHash;
        public List<Location> neighbors;
        public byte[] rgbValues;
        public byte[] elp;
        public int bmpWidth;
        public int bmpHeight;
        public int stride;
        public byte blackThreshHold;
    }

    class HeightMap4Main
    {
        static int PL_METADATA_LENGH = 1;

        static Form1 form = null;
        static TextBox textBox = null;

        public static void setFormStuff(Form1 f, TextBox t)
        {
            form = f;
            textBox = t;
        }

        public static void Start(string[] args)
        {
            string mode = args[0];

            if (mode == "bw")
                BlacknWhite(args);
            else
            if (mode == "pl1" || mode == "pl1check")
                Pallete1(args);
            else
            if (mode == "add")
                Add(args);
            else
            if (mode == "k200")
            {
                //Ants(args);
                // need alot of heap for man many recursive calls, so do it via a new thread
                Thread T = new Thread(() => Ants(args), 1000000000);
                T.Start();
            }
            else if (mode == "one_color_modify")
                OneColorModify(args);
        }


        const byte ELP_MEMBER = 1;
        public static int antThreshold;


        private static void Ants(string[] args)
        {
            // read input file
            // L1: loop over all pixels left -> right , up -> bottom
            // search for pixel to start groupFlood ,needs to be "black" (have func isBlack())
            // create new groupHash, P1: call groupFlood with this pixel
            // groupFlood: add current pixel to groupHash 
            // get neighbors  , (8 max) foreach neighbor pixel - check if it is "black" and not
            // already in groupHash and not an elephant member (see below) , 
            // if yes call groupFlood with this pixel to add it and 
            // continue flooding
            // when P1 returns in the L1 loop - check the size of groupHash, and decide if
            // group is an Ant (too small).
            // If yes: mark all ant memebers as white in the bitmap array
            // If no - this is an elephant. Mark each pixel in elephant as such - elephant member
            // use additional byte for each pixel for this (so not to destroy its exact color
            // as we are keeping these intact)

            // open debug file
            //sw = File.CreateText("ants_debug.txt");

            string fname = args[1];
            Bitmap bmp = new Bitmap(fname);

            antThreshold = 100;
            if (args.Length > 2)
                antThreshold = Convert.ToByte(args[2]);


            byte blackThreshHold = 160;
            if (args.Length > 3)
                blackThreshHold = Convert.ToByte(args[3]);

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            // create array for elephant pixel marking, 1 byte per pixel
            int ElpBytes = bmpData.Width * bmp.Height;
            byte[] elp = new byte[ElpBytes];

            List<Location> n = new List<Location>();
            n.Add(new Location() { x = -1, y = -1 });
            n.Add(new Location() { x = -1, y = 0 });
            n.Add(new Location() { x = -1, y = 1 });
            n.Add(new Location() { x = 0, y = -1 });
            n.Add(new Location() { x = 0, y = 1 });
            n.Add(new Location() { x = 1, y = -1 });
            n.Add(new Location() { x = 1, y = 0 });
            n.Add(new Location() { x = 1, y = 1 });

            // alot of not changing data needed for the GroupFlood recursive function
            GroupFloodData gfd = new GroupFloodData();
            gfd.rgbValues = rgbValues;
            gfd.elp = elp;
            gfd.bmpWidth = bmp.Width;
            gfd.bmpHeight = bmp.Height;
            gfd.neighbors = n;
            gfd.stride = bmpData.Stride;
            gfd.blackThreshHold = blackThreshHold;

            // loop on all pixels
            int stride = bmpData.Stride;
            for (int x = 0; x < bmpData.Width; x++)
            {
                for (int y = 0; y < bmpData.Height; y++)
                {
                    byte r = rgbValues[(y * stride) + (x * 3)];
                    byte g = rgbValues[(y * stride) + (x * 3) + 1];
                    byte b = rgbValues[(y * stride) + (x * 3) + 2];
                    byte e = elp[y * bmpData.Width + x];



                    // find a pixel to start flooding a group from
                    // check colors bellow threshHold and 
                    // not neighbor of elp (if neighbor of elp - make him an elp as well)
                    bool isBlack = (r < blackThreshHold && g < blackThreshHold && b < blackThreshHold);
                    if (isBlack)
                    {
                        // following block checks if the pixel is an elp group member due to neighboehood with other elp member
                        bool isElpMember = false;
                        foreach (var nn in gfd.neighbors)
                        {
                            int xn = x + nn.x;
                            int yn = y + nn.y;
                            if (xn >= 0 && xn <= bmp.Width - 1 && yn >= 0 && yn <= bmp.Height - 1)
                            {
                                // I think it is OK not test on himself
                                isElpMember = elp[yn * bmpData.Width + xn] == ELP_MEMBER;
                                if (isElpMember)
                                {
                                    elp[y * bmpData.Width + x] = ELP_MEMBER; // make him an elp member if he has an elp neighbor
                                    break;
                                }
                            }
                        }
                        if (!isElpMember)
                        {


                            if (x % 100 == 0 && y % 100 == 0)
                                MyDebug("First Pixel of Group: " + x.ToString() + "," + y.ToString());


                            // new dictionary list for each group under test
                            Dictionary<string, Location> groupHash = new Dictionary<string, Location>();
                            gfd.groupHash = groupHash;
                            GroupFlood(gfd, new Location() { x = x, y = y }, depth: 0);

                            int groupSize = groupHash.Count;
                            bool isAnt = groupSize < antThreshold;
                            //    MyDebug("new group with groupSize: " + groupSize.ToString() + (isAnt ? " ANT!!!!" : ""));
                            if (isAnt) // we have an ant
                            {
                                foreach (var item in groupHash)
                                { // set all ant pixels to white

                                    Location p = item.Value;
                                    rgbValues[(p.y * stride) + (p.x * 3)] = 255;
                                    rgbValues[(p.y * stride) + (p.x * 3) + 1] = 255;
                                    rgbValues[(p.y * stride) + (p.x * 3) + 2] = 255;
                                }
                            }
                            else
                            {
                                foreach (var item in groupHash)
                                { // mark all pixels as elephant members, leave them as is in original image
                                    Location p = item.Value;
                                    elp[p.y * bmpData.Width + p.x] = ELP_MEMBER;
                                }
                            }
                        }
                    }
                }
            }


            // create bitmap back from new values
            // Commit the changes, and unlock the 50x30 portion of the bitmap.  
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bmp.UnlockBits(bmpData);


            string[] s = fname.Split('.');
            string newName = string.Concat(s[0], "_size_", antThreshold.ToString(), "_blackLevel_", blackThreshHold.ToString(), ".", s[1]);
            bmp.Save(newName, ImageFormat.Jpeg);

            MyDebug("done!!");
        }

        static void GroupFlood(GroupFloodData gfd, Location pixel, int depth)
        {
            // add current pixel to the group
            gfd.groupHash.Add(pixel.key(), pixel);
            //if (gfd.groupHash.Count % 1000 == 0)
            //    MyDebug("groupSize now is " + gfd.groupHash.Count.ToString() + "     " + pixel.x.ToString() + "," + pixel.y.ToString());


            //if (depth % 100 == 0)
            //MyDebug("Recursive Depth now is " + depth.ToString());

            //Debug.WriteLine("added to group: " + pixel.x.ToString() + "," + pixel.y.ToString());

            // recursive call all neighbors of current pixel that are part of the group
            foreach (var n in gfd.neighbors)
            {
                int x = pixel.x + n.x;
                int y = pixel.y + n.y;
                Location loc = new Location() { x = x, y = y };
                //MyDebug("trying neighbor: " + x.ToString() + "," + y.ToString());
                // first check the potential neighbor is in the bitmap
                if (x >= 0 && x <= gfd.bmpWidth - 1 && y >= 0 && y <= gfd.bmpHeight - 1)
                {
                    //MyDebug("neighbor in bitmap: " + x.ToString() + "," + y.ToString());

                    // now check it is a new group memeber
                    byte r = gfd.rgbValues[(y * gfd.stride) + (x * 3)];
                    byte g = gfd.rgbValues[(y * gfd.stride) + (x * 3) + 1];
                    byte b = gfd.rgbValues[(y * gfd.stride) + (x * 3) + 2];
                    byte e = gfd.elp[y * gfd.bmpWidth + x];

                    //MyDebug("rgbe: " + r.ToString() + "," + g.ToString() + "," + b.ToString() + "," + e.ToString());

                    // check the pixel color is OK, that pixel is not part of an alread detected
                    // elephant (group thatr is not an ant) and that this pixel is not already part
                    // of the current group
                    if (r < gfd.blackThreshHold && g < gfd.blackThreshHold && b < gfd.blackThreshHold
                        && e != ELP_MEMBER && !gfd.groupHash.ContainsKey(loc.key()))
                    {
                        // we only collect up to ant group size or a bit more (5) pixels
                        if (depth < antThreshold + 5)
                            GroupFlood(gfd, loc, depth + 1); // recursive call
                    }
                }
            }
        }

        // support writing from a different thread to the UI
        delegate void StringArgReturningVoidDelegate(string text);
        static void MyDebug(string s)
        {
            //Debug.WriteLine(s);
            //Console.WriteLine(s);
            s = s + Environment.NewLine;
            if (textBox.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(MyDebug);
                form.Invoke(d, new object[] { s });
            }
            else
            {
                //textBox.Text += s;
                textBox.AppendText(s);
            }
        }

        static void Add(string[] args)
        {
            // get 2 arrays of both files
            // iterate them together and sum up into a third array
            // write back the third array


            // file #1
            string fname = args[1];
            Bitmap bmp = new Bitmap(fname);
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);


            // file #2
            string fname2 = args[2];
            Bitmap bmp2 = new Bitmap(fname2);
            // Lock the bitmap's bits.  
            Rectangle rect2 = new Rectangle(0, 0, bmp2.Width, bmp2.Height);
            BitmapData bmpData2 = bmp2.LockBits(rect2, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            // Get the address of the first line.
            IntPtr ptr2 = bmpData2.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            int bytes2 = bmpData2.Stride * bmp2.Height;
            byte[] rgbValues2 = new byte[bytes2];
            // Copy the RGB values into the array.
            Marshal.Copy(ptr2, rgbValues2, 0, bytes2);


            if (bytes != bytes2)
            {
                ExitMsg("different number of pixels in files: color:" + bytes.ToString() + "  greyscale:" + bytes2.ToString());
            }

            double ratioFile1 = Convert.ToDouble(args[3]);
            double ratioFile2 = 1;
            double weightFile1 = ratioFile1 / (ratioFile1 + ratioFile2);
            double weightFile2 = ratioFile2 / (ratioFile1 + ratioFile2);


            // itterate the values and change by algorithm
            int stride = bmpData.Stride;
            for (int x = 0; x < bmpData.Width; x++)
            {
                for (int y = 0; y < bmpData.Height; y++)
                {
                    byte r = rgbValues[(y * stride) + (x * 3)];
                    byte g = rgbValues[(y * stride) + (x * 3) + 1];
                    byte b = rgbValues[(y * stride) + (x * 3) + 2];

                    byte r2= rgbValues2[(y * stride) + (x * 3)];
                    byte g2= rgbValues2[(y * stride) + (x * 3) + 1];
                    byte b2= rgbValues2[(y * stride) + (x * 3) + 2];


                    double dr3 = r * weightFile1 + r2 * weightFile2;
                    double dg3 = g * weightFile1 + g2 * weightFile2;
                    double db3 = b * weightFile1 + b2 * weightFile2;

                    if (dr3 > 255) dr3 = 255;
                    if (dg3 > 255) dg3 = 255;
                    if (db3 > 255) db3 = 255;

                    byte r3 = Convert.ToByte(dr3);
                    byte g3 = Convert.ToByte(dg3);
                    byte b3 = Convert.ToByte(db3);


                    rgbValues[(y * stride) + (x * 3)] = r3;
                    rgbValues[(y * stride) + (x * 3) + 1] = g3;
                    rgbValues[(y * stride) + (x * 3) + 2] = b3;

                }
            }

            // create bitmap back from new values
            // Commit the changes, and unlock the 50x30 portion of the bitmap.  
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bmp.UnlockBits(bmpData);

            string[] s = fname.Split('.');
            string newName = string.Concat(s[0], "_added", ".", s[1]);
            MyDebug("save composit file to: " + newName);
            bmp.Save(newName, ImageFormat.Jpeg);
        }

        static void  BlacknWhite(string[] args) {
            string fname = args[1];
            byte rThresh = args.Length>2 ? Convert.ToByte(args[2]) : (byte)20;
            byte gThresh = args.Length>3 ? Convert.ToByte(args[3]) : (byte)20;
            byte bThresh = args.Length>4 ? Convert.ToByte(args[4]) : (byte)20;

            Bitmap bmp = new Bitmap(fname);

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            //byte[][] r = new byte[bmpData.Width][];  // each is a column with height 1
            //byte[][] g = new byte[bmpData.Width][];
            //byte[][] b = new byte[bmpData.Width][];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);

           

            // itterate the values and change by algorithm
            int stride = bmpData.Stride;
            for (int x = 0; x < bmpData.Width; x++)
            {
                for (int y = 0; y < bmpData.Height; y++)
                {
                    byte r = rgbValues[(y * stride) + (x * 3)];
                    byte g = rgbValues[(y * stride) + (x * 3) + 1];
                    byte b = rgbValues[(y * stride) + (x * 3) + 2];

                    if (r<rThresh && g<gThresh && b<bThresh)
                    {
                        r = g = b = 0;
                    }
                    else
                    {
                        r = g = b = 255;
                    }
                    rgbValues[(y * stride) + (x * 3)] = r;
                    rgbValues[(y * stride) + (x * 3)  +1] = g;
                    rgbValues[(y * stride) + (x * 3) + 2] = b;
                }
            }


            // create bitmap back from new values
            // Commit the changes, and unlock the 50x30 portion of the bitmap.  
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bmp.UnlockBits(bmpData);


            string[] s = fname.Split('.');
            string newName = string.Concat(s[0], "_new_", rThresh.ToString() , ".", s[1]);

            bmp.Save(newName, ImageFormat.Jpeg);
            MyDebug("done. Created file: " + newName);

            // Display the altered bitmap.
            //Graphics.DrawImage(bmp, 150, 10);


            // good reference to see the result immediatly in a winform
            // https://msdn.microsoft.com/en-us/library/system.drawing.bitmap.unlockbits%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396


            //for (int x = 0; x < bmpData.Width; x++)
            //{
            //    r[x] = new byte[bmp.Height]; // column of full height (vertical line)
            //    g[x] = new byte[bmp.Height];
            //    b[x] = new byte[bmp.Height];
            //    for (int y = 0; y < bmpData.Height; y++)
            //    {
            //        b[x][y] = (byte)(rgbValues[(y * stride) + (x * 3)]);
            //        g[x][y] = (byte)(rgbValues[(y * stride) + (x * 3) + 1]);
            //        r[x][y] = (byte)(rgbValues[(y * stride) + (x * 3) + 2]);
            //    }
            //}
        }

        static void Pallete1(string[] args)
        {
            bool checkMode = (args[0] == "pl1check");
            string imgFname=null;
            int red=0, green=0, blue=0;

            if (checkMode)
            {
                red = Convert.ToInt32(args[2]);
                green = Convert.ToInt32(args[3]);
                blue = Convert.ToInt32(args[4]);
            }
            else
            {
                imgFname = args[2];
            }


            string palleteFname = args[1];
            if (!palleteFname.Contains(".txt"))
                palleteFname += ".txt";
            string palleteBinFname = palleteFname.Replace(".txt", ".bin");

            // load the color->pallete relation file if exists and is older than the pallete texdt file, or if check mode
            byte[] pl;
            if (File.Exists(palleteBinFname) && File.GetLastWriteTime(palleteBinFname) > File.GetLastWriteTime(palleteFname) && !checkMode)
            {
                pl = File.ReadAllBytes(palleteBinFname);
                MyDebug("read pallete binary file");
            }
            else
            {
                // otherwise load the text pallete file and create list of palletes
                // parse pallete
                // file format R,G,B
                //
                // 0,0,0  255,0,0
                //
                // 10,10,10
                int bytes = PL_METADATA_LENGH + 256 * 256 * 256;
                List<List<Color>> palletes = LoadPalleteTextFile(palleteFname);
                MyDebug("read pallete text file");

                int numPalletes = palletes.Count;
                MyDebug("found " + numPalletes.ToString() + " palletes");

                if (checkMode)
                {
                    int closestPallete = ClosestPallete(Color.FromArgb(red, green, blue), palletes, writeToConsole:true);
                    ExitMsg("----");
                }

                // calculate the color->pallete relationship and save for future use
                //  for each possible color, find the nearest pallete
                // create rgb array 255x255x255 (2^24 ~ 16M entries) for each color - what pallete it belongs to
                MyDebug("calculating pallete color relationship ...");
                pl = new byte[bytes];

                // save the number of palletes in the meta data (first byte of file)
                pl[0] = Convert.ToByte(numPalletes);

                for (int r = 0; r <= 255; r++)
                    for (int g = 0; g <= 255; g++)
                        for (int b = 0; b <= 255; b++)
                        {
                            int idx = r + (g << 8) + (b << 16);
                            pl[PL_METADATA_LENGH + idx] = Convert.ToByte( ClosestPallete(Color.FromArgb(r,g,b), palletes));
                        }

                // write the color->pallete relation in a binary file for quick load next time
                BinaryWriter binWriter = new BinaryWriter(new MemoryStream());

                // Write the data to the stream.
                File.WriteAllBytes(palleteBinFname, pl);
                File.SetLastWriteTime(palleteBinFname, DateTime.Now);
                MyDebug("wrote pallete binary file");
            }


            // pl holds the relationship
            // for each pixel color in the image - get its pallete , then change its color to the palletes color

            Bitmap bmp = new Bitmap(imgFname);
            MyDebug("starting to create height map");
            Pallete1DoCreateHeightMap(bmp, pl);
            string[] s = imgFname.Split('.');
            string newName = string.Concat(s[0], "_new", ".", s[1]);
            bmp.Save(newName, ImageFormat.Jpeg);
            MyDebug("saved heightmap to file " + newName);
        }


        // resize /10 in paint.net and resized back when done otherwize crashes.
        // even quality 7 in paint.net is good
        // rgb - blue is first in byte array
        // last sent to ray - sensativity 130 , divide by 2 to make clouds darker (smaller value), added check it is not bluish (blueih == blue larger tahn green & red by 10
        private static void OneColorModify(string[] args)
        {
            // get 2 files, color
            // open both files 
            // scan real image, find pixels with color (with some sensativity)
            // for each found modify the respective pixel in the height map file - flip the height 

            int[] color = args[1].Split(',').Select(c => Convert.ToInt32(c)).ToArray();
            int sensativity = Convert.ToInt32(args[2]) ; 

            // image file
            string fname = args[3];
            Bitmap bmp = Image.FromFile(fname) as Bitmap;
            //Bitmap bmp = new Bitmap(fname);
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            // height file
            string fname2 = args[4];
            Bitmap bmp2 = new Bitmap(fname2);
            // Lock the bitmap's bits.  
            Rectangle rect2 = new Rectangle(0, 0, bmp2.Width, bmp2.Height);
            BitmapData bmpData2 = bmp2.LockBits(rect2, ImageLockMode.ReadWrite,  PixelFormat.Format24bppRgb  );
            // Get the address of the first line.
            IntPtr ptr2 = bmpData2.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            int bytes2 = bmpData2.Stride * bmp2.Height;
            byte[] rgbValues2 = new byte[bytes2];
            // Copy the RGB values into the array.
            Marshal.Copy(ptr2, rgbValues2, 0, bytes2);


            if (bytes != bytes2)
            {
                ExitMsg("different number of pixels in files: image:" + bytes.ToString() + "  heights:" + bytes2.ToString());
                return;
            }


            // itterate the height values 
            int stride = bmpData2.Stride;
            for (int x = 0; x < bmpData2.Width; x++)
            {
                for (int y = 0; y < bmpData2.Height; y++)
                {
                    try
                    {
                        // height color
                        int loc = (y * stride) + (x * 3);
                        byte r2 = rgbValues2[loc];
                        byte g2 = rgbValues2[loc + 1];
                        byte b2 = rgbValues2[loc + 2];

                        // check if pixel within sensativity closefulness to color
                        if (color[0] > r2 - sensativity && color[0] < r2 + sensativity &&
                            color[1] > g2 - sensativity && color[1] < g2 + sensativity &&
                            color[2] > b2 - sensativity && color[2] < b2 + sensativity)
                        {

                            byte b = rgbValues[loc];
                            byte g = rgbValues[loc + 1];
                            byte r = rgbValues[loc + 2];

                            // if it is not too blue
                            bool isBluish = ((b - r > 10) && (b - g > 10));
                            if (!isBluish) {
                                // raise the height
                                // height color
                                //MyDebug($"{x},{y} color: {rgbValues2[loc]},{ rgbValues2[loc + 1]},{rgbValues2[loc + 2]}");
                                rgbValues2[loc] = (byte)((255 - rgbValues2[loc]) / 2);
                                rgbValues2[loc + 1] = (byte)((255 - rgbValues2[loc + 1]) / 2);
                                rgbValues2[loc + 2] = (byte)((255 - rgbValues2[loc + 2]) / 2);
                                //MyDebug($"{x},{y} newcolor: {rgbValues2[loc]},{ rgbValues2[loc + 1]},{rgbValues2[loc + 2]}");
                                //MyDebug("");
                            }
                        }
                    }
                    catch (Exception e) {
                        // just go on
                    }
                }
            }


            // create bitmap back from new values
            // Commit the changes, and unlock the 50x30 portion of the bitmap.  
            Marshal.Copy(rgbValues2, 0, ptr2, bytes2);
            bmp2.UnlockBits(bmpData2);

            string[] s = fname2.Split('.');
            string newName = string.Concat(s[0], "_modified3", ".", s[1]);
            MyDebug("save modified height file to: " + newName);


            bmp2.Save(newName, ImageFormat.Jpeg);


            //string outputFileName = newName;
            //using (MemoryStream memory = new MemoryStream())
            //{
            //    using (FileStream fs = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite))
            //    {
            //        bmp2.Save(memory, ImageFormat.Bmp);
            //        byte[] bytes = memory.ToArray();
            //        fs.Write(bytes, 0, bytes.Length);
            //    }
            //}




        }


        static void Pallete1DoCreateHeightMap(Bitmap bmp, byte[] pl)
        {
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            
            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            // get number of palletes and calculate delta height between palletes
            // 2 palletes - delta is 256/1=256 (0,256) 
            // 3 palletes - delta is 256/2 = 128 (0,128,256)
            // 4 palletes - delta is 256/3 
            // 256 will always be converted to 255
            int numPalletes = pl[0];
            int delta = 256 / (numPalletes - 1);

            // itterate the values and change by algorithm
            int stride = bmpData.Stride;
            for (int x = 0; x < bmpData.Width; x++)
            {
                for (int y = 0; y < bmpData.Height; y++)
                {
                    // get r,g,b value of pixel
                    byte b = rgbValues[(y * stride) + (x * 3)];
                    byte g = rgbValues[(y * stride) + (x * 3) + 1];
                    byte r = rgbValues[(y * stride) + (x * 3) + 2];

                    // get the pallete this pixel belongs to
                    int pidx = PL_METADATA_LENGH + r + (g << 8) + (b << 16);
                    byte pallete = pl[pidx];

                    // get the r,g,b height color of this pallete (grey level)
                    int tmp = pallete * delta;
                    r = g = b = Convert.ToByte(tmp==256 ? 255 : tmp);

                    // write back the new r,g,b to the original rgb array
                    rgbValues[(y * stride) + (x * 3)] = r;
                    rgbValues[(y * stride) + (x * 3) + 1] = g;
                    rgbValues[(y * stride) + (x * 3) + 2] = b;
                }
            }

            // create bitmap back from new values
            // Commit the changes, and unlock the 50x30 portion of the bitmap.  
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bmp.UnlockBits(bmpData);
        }

        static int ClosestPallete( Color color, List<List<Color>> palletes, bool writeToConsole=false)
        {
            int matchedPallete=0;
            double matchedPalleteDistance=1000000000000000;
            Color matchedColor = new Color();
            int p = 0;
            foreach (var pallete in palletes)
            {
                foreach (var pcolor in pallete)
                {
                    double d = dist(pcolor, color);
                    if (d <= matchedPalleteDistance)
                    {
                        matchedPallete = p;
                        matchedPalleteDistance = d;
                        matchedColor = pcolor;
                    }
                }
                p++;
            }
            if (writeToConsole)
            {
                MyDebug("checking Color: " + color.ToString());
                MyDebug("matched to pallete: " + matchedPallete + "  (0 is first pallete), due to color: " + matchedColor.ToString());
            }
            return matchedPallete;
        }

        static double dist(Color c1, Color c2)
        {
            return Math.Sqrt( (c1.R - c2.R)*(c1.R - c2.R) + (c1.G - c2.G) * (c1.G - c2.G) + (c1.B - c2.B) * (c1.B - c2.B)); 
        }

        static List<List<Color>> LoadPalleteTextFile(string palleteFname) {

            List<List<Color>> palletes = new List<List<Color>>();
            StreamReader file = new StreamReader(palleteFname);
            string line;
            int i = 0;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length<2 || line.Substring(0, 2) == "//")
                {
                    i++;
                    continue;
                }
                string[] colors = line.Split(' ');
                colors = colors.Where(c => c.Length > 0).ToArray();
                List<Color> palleteColors = new List<Color>();
                foreach (var c in colors)
                {
                    string[] rgb = c.Split(',');
                    if (rgb.Length != 3)
                        ExitMsg(string.Concat("wrong color ", c, " in line ", i.ToString(), "rgb length: ", rgb.Length));
                    Color color = Color.FromArgb(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
                    palleteColors.Add(color);
                }
                i++;
                palletes.Add(palleteColors);
            }
            file.Close();
            return palletes;
        }

        static void ExitMsg(string msg)
        {
            MyDebug(msg);
            //Environment.Exit(0);
        }

    }
}
