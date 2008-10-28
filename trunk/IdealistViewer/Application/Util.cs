using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using OpenMetaverse;

namespace IdealistViewer
{
    public class Util
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetOS()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                if (File.Exists("/System/Library/Frameworks/Cocoa.framework/Cocoa"))
                {
                    return "osx";
                }
                else
                {
                    return "unix";
                }
            }
            else
            {
                return "win";
            }
        }

        public static string ApplicationDataDirectory
        {
            get
            {
                switch (GetOS())
                {
                    case "osx": return Environment.GetEnvironmentVariable("HOME") + "/Library/Application Support/OpenViewer";
                    case "unix": return Environment.GetEnvironmentVariable("HOME") + "/.openviewer";
                    case "win": return Environment.GetEnvironmentVariable("APPDATA") + "/OpenViewer";

                    default:
                        m_log.Warn("Unable to determine proper application data directory for this operating system.");
                        return ".";
                }
            }
        }
        public static T Clamp<T>(T x, T min, T max)
            where T : System.IComparable<T>
        {
            return x.CompareTo(max) > 0 ? max :
                x.CompareTo(min) < 0 ? min :
                x;
        }

        public static string MakePath(params string[] elements)
        {
            if (elements.Length == 0)
                return string.Empty;

            string path = elements[0];

            for (int i = 1; i < elements.Length; ++i)
            {
                path = Path.Combine(path, elements[i]);
            }

            return path;
        }

        public static ImageCodecInfo GetImageEncoder(string imageType)
        {
            imageType = imageType.ToUpperInvariant();

            foreach (ImageCodecInfo info in ImageCodecInfo.GetImageEncoders())
            {
                if (info.FormatDescription == imageType)
                {
                    return info;
                }
            }
            return null;
        }

        /// <summary>
        /// Saves bmp for the terrain
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="filename"></param>
        public static void SaveBitmapToFile(Bitmap bitmap, string filename)
        {
            ImageCodecInfo bmpEncoder = GetImageEncoder("BMP");

            //EncoderParameters parms = new EncoderParameters(1);
            //parms.Param[0] = new EncoderParameter(Encoder.ColorDepth, 32L);

            Bitmap resize = new Bitmap(bitmap);
            //resize.RotateFlip(RotateFlipType.RotateNoneFlipY);
            resize.RotateFlip(RotateFlipType.RotateNoneFlipX);
            resize.Save( filename, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        /// <summary>
        /// Splits the Firstname and last name via space.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="firstname"></param>
        /// <param name="lastname"></param>
        public static void separateUsername(string username, out string firstname, out string lastname)
        {
            int index = username.IndexOf(" ");

            if (index == -1)
            {
                firstname = username.Trim();
                lastname = String.Empty;
            }
            else
            {
                firstname = username.Substring(0, index).Trim();
                lastname = username.Substring(index + 1).Trim();
            }
        }

        /// <summary>
        /// Fix invalid loginURIs
        /// </summary>
        /// <param name="loginURI"></param>
        /// <returns></returns>
        public static string getSaneLoginURI(string loginURI)
        {
            // libSL requires the login URI to begin with "http://" or "https://"

            Regex re = new Regex("://");
            string[] parts = re.Split(loginURI.Trim());

            if (parts.Length > 1)
            {
                if (parts[0].ToLower() == "http" || parts[0].ToLower() == "https")
                    return loginURI;
                else
                    return "http://" + parts[1];
            }
            else
                return "http://" + loginURI;
        }

        public static string configDir()
        {
            return ".";
        }

        /// <summary>
        /// Offsets a position by the Global position determined by the regionhandle
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector3 OffsetGobal(ulong regionHandle, Vector3 pos)
        {

            uint locationx = 0;
            uint locationy = 0;
            Utils.LongToUInts(regionHandle, out locationx, out locationy);
            pos.X = (int)locationx + pos.X;
            pos.Y = (int)locationy + pos.Y;

            return pos;
        }

        /// <summary>
        /// Fix to the fact that Microsoft only provides int RGB
        /// :D
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color FromArgbf(float a, float r, float g, float b)
        {
            return Color.FromArgb((byte)(255 * a), (byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
        }

        /// <summary>
        /// Converts byte values into the more complicated 32 bit constructor
        /// Eevil :D
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            return Color.FromArgb((int)(a << 24 | r << 16 | g << 8 | b));
        }


        /// <summary>
        /// Enhance the version string with extra information if it's available.
        /// </summary>
        public static string EnhanceVersionInformation()
        {
            string buildVersion = string.Empty;
            string m_version = string.Empty;
            // Add subversion revision information if available
            // Try file "svn_revision" in the current directory first, then the .svn info.
            // This allows to make the revision available in simulators not running from the source tree.
            // FIXME: Making an assumption about the directory we're currently in - we do this all over the place
            // elsewhere as well
            string svnRevisionFileName = "svn_revision";
            string svnFileName = ".svn/entries";
            string inputLine;
            int strcmp;

            if (File.Exists(svnRevisionFileName))
            {
                StreamReader RevisionFile = File.OpenText(svnRevisionFileName);
                buildVersion = RevisionFile.ReadLine();
                buildVersion.Trim();
                RevisionFile.Close();
            }

            if (string.IsNullOrEmpty(buildVersion) && File.Exists(svnFileName))
            {
                StreamReader EntriesFile = File.OpenText(svnFileName);
                inputLine = EntriesFile.ReadLine();
                while (inputLine != null)
                {
                    // using the dir svn revision at the top of entries file
                    strcmp = String.Compare(inputLine, "dir");
                    if (strcmp == 0)
                    {
                        buildVersion = EntriesFile.ReadLine();
                        break;
                    }
                    else
                    {
                        inputLine = EntriesFile.ReadLine();
                    }
                }
                EntriesFile.Close();
            }

            m_version += string.IsNullOrEmpty(buildVersion) ? ".00000" : ("." + buildVersion + "     ").Substring(0, 6);

            // Add operating system information if available
            string OSString = "";

            //if (System.Environment.OSVersion.Platform != PlatformID.Unix)
            //{
            //    OSString = System.Environment.OSVersion.ToString();
            //}
            //else
            //{
            //    OSString = Util.ReadEtcIssue();
            //}

            if (OSString.Length > 45)
            {
                OSString = OSString.Substring(0, 45);
            }

            m_version += " (OS " + OSString + ")";
            return m_version;
        }
    }
}
