using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace IdealistViewer
{
    
    class SculptMeshLOD : IDisposable
    {
        Image idata = null;
        Bitmap bLOD = null;
        Bitmap bBitmap = null;
        private int lod = 32;

        public int Scale
        {
            get
            {
                return lod;
            }
        }

        public Bitmap ResultBitmap
        {
            get { return bLOD; }
        }

        public int LOD
        {
            get
            {
                return (int)Math.Log(Scale, 2);
            }
            set
            {
                int power = value;
                if (power == 0)
                    power = 6;
                if (power < 2)
                    power = 2;
                if (power > 9)
                    power = 9;
                int t = (int)Math.Pow(2, power);
                if (t != Scale)
                {
                    lod = t;
                }
            }
        }

        public SculptMeshLOD(Bitmap oBitmap, float _lod)
        {
            if (_lod == 2f || _lod == 4f || _lod == 8f || _lod == 16f || _lod == 32f || _lod == 64f)
                lod = (int)_lod;

            bBitmap = new Bitmap(oBitmap);
            if (bBitmap.Width == bBitmap.Height)
            {
                DoLOD();
            }
            else
            {
                System.Console.WriteLine("[SCULPT]: Unable to use a bad sculpt mesh.");
            }

        }
        private void DoLOD()
        {
            int x_max = Math.Min(Scale, bBitmap.Width);
            int y_max = Math.Min(Scale, bBitmap.Height);
            if (bBitmap.Width == x_max && bBitmap.Height == y_max)
                bLOD = bBitmap;

            else if (bLOD == null || x_max != bLOD.Width || y_max != bLOD.Height)//don't resize if you don't need to.
            {
                System.Drawing.Bitmap tile = new System.Drawing.Bitmap(bBitmap.Width * 2, bBitmap.Height, PixelFormat.Format24bppRgb);
                System.Drawing.Bitmap tile_LOD = new System.Drawing.Bitmap(x_max * 2, y_max, PixelFormat.Format24bppRgb);

                bLOD = new System.Drawing.Bitmap(x_max, y_max, PixelFormat.Format24bppRgb);
                bLOD.SetResolution(bBitmap.HorizontalResolution, bBitmap.VerticalResolution);

                System.Drawing.Graphics grPhoto = System.Drawing.Graphics.FromImage(tile);
                grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                grPhoto.DrawImage(bBitmap,
                    new System.Drawing.Rectangle(0, 0, bBitmap.Width / 2, bBitmap.Height),
                    new System.Drawing.Rectangle(bBitmap.Width / 2, 0, bBitmap.Width / 2, bBitmap.Height),
                    System.Drawing.GraphicsUnit.Pixel);

                grPhoto.DrawImage(bBitmap,
                    new System.Drawing.Rectangle((3 * bBitmap.Width) / 2, 0, bBitmap.Width / 2, bBitmap.Height),
                    new System.Drawing.Rectangle(0, 0, bBitmap.Width / 2, bBitmap.Height),
                    System.Drawing.GraphicsUnit.Pixel);

                grPhoto.DrawImage(bBitmap,
                    new System.Drawing.Rectangle(bBitmap.Width / 2, 0, bBitmap.Width, bBitmap.Height),
                    new System.Drawing.Rectangle(0, 0, bBitmap.Width, bBitmap.Height),
                    System.Drawing.GraphicsUnit.Pixel);

                grPhoto = System.Drawing.Graphics.FromImage(tile_LOD);
                grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                grPhoto.DrawImage(tile,
                    new System.Drawing.Rectangle(0, 0, tile_LOD.Width, tile_LOD.Height),
                    new System.Drawing.Rectangle(0, 0, tile.Width, tile.Height),
                    System.Drawing.GraphicsUnit.Pixel);

                grPhoto = System.Drawing.Graphics.FromImage(bLOD);
                grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                grPhoto.DrawImage(tile_LOD,
                    new System.Drawing.Rectangle(0, 0, bLOD.Width, bLOD.Height),
                    new System.Drawing.Rectangle(tile_LOD.Width / 4, 0, tile_LOD.Width / 2, tile_LOD.Height),
                    System.Drawing.GraphicsUnit.Pixel);

                grPhoto.Dispose();
                tile_LOD.Dispose();
                tile.Dispose();
            }

        }
        
    
#region IDisposable Members

        public void  Dispose()
        {
            bBitmap.Dispose();
        }

#endregion
    }
}
