using System;
using System.IO;
using System.Windows;

namespace TerrainPreprocessor
{
    struct AreaRange
    {
        public Point minPt { get; set; }
        public Point maxPt { get; set; }
    }

    class DemController
    {
        public int FileWidth { get; set; }
        public int FileHeight { get; set; }
        
        public short[,] ResizeHeightMap(short[,] heightMap, int resolution)
        {
            short[,] resizeMap = new short[resolution, resolution];

            float dy = (float)(heightMap.GetLength(0)) / resolution;
            float dx = (float)(heightMap.GetLength(1)) / resolution;

            // Create bilinear interpolated height map data
            for (int ry = 0; ry < resolution; ry++)
            {
                int hy = (int)Math.Floor(ry * dy);
                float ratioY = ry * dy - hy;

                for (int rx = 0; rx < resolution; rx++)
                {
                    int hx = (int)Math.Floor(rx * dx);
                    float ratioX = rx * dx - hx;

                    // Out of index exception handling
                    if (hy >= heightMap.GetLength(0) - 1) hy = heightMap.GetLength(0) - 2;
                    if (hx >= heightMap.GetLength(1) - 1) hx = heightMap.GetLength(1) - 2;

                    float lerpU1 = Lerp(heightMap[hy, hx], heightMap[hy, hx + 1], ratioX);
                    float lerpU2 = Lerp(heightMap[hy + 1, hx], heightMap[hy + 1, hx + 1], ratioX);
                    float lerpUV = Lerp(lerpU1, lerpU2, ratioY);

                    resizeMap[ry, rx] = (short)lerpUV;
                }
            }

            return resizeMap;
        }

        public short[,] ConvCustom(short[,] heightMap)
        {
            for (int i = 0; i< heightMap.GetLength(0); i++)
            {
                for (int j = 0; j < heightMap.GetLength(1); j++)
                {
                    if (heightMap[i, j] < 0)
                        heightMap[i, j] = 0;
                    else if (heightMap[i, j] > 2000)
                        heightMap[i, j] = 2000;
                }
            }
            return heightMap;
        }

        public short[,] ClipSpecificArea(short[,] minmin, short[,] minmax, short[,] maxmin, short[,] maxmax, AreaRange areaRange)
        {
            int originY = (int)((areaRange.minPt.Y - (int)areaRange.minPt.Y) * FileHeight);
            int originX = (int)((areaRange.minPt.X - (int)areaRange.minPt.X) * FileWidth);
            int destY = (int)((areaRange.maxPt.Y - (int)areaRange.minPt.Y) * FileHeight);
            int destX = (int)((areaRange.maxPt.X - (int)areaRange.minPt.X) * FileWidth);

            short[,] heightMap = new short[destY - originY, destX - originX];

            // Load specific height map data from textures.
            for (int y = FileHeight; y < destY; y++)
            {
                for (int x = originX; x < ((FileWidth < destX) ? FileWidth : destX); x++)
                {
                    heightMap[y - originY, x - originX] = minmin[y - FileHeight, x];
                }
                for (int x = FileWidth; x < destX; x++)
                {
                    heightMap[y - originY, x - originX] = maxmin[y - FileHeight, x - FileWidth];
                }
            }
            for (int y = originY; y < ((FileHeight < destY) ? FileHeight : destY); y++)
            {
                for (int x = originX; x < ((FileWidth < destX) ? FileWidth : destX); x++)
                {
                    heightMap[y - originY, x - originX] = minmax[y, x];
                }
                for (int x = FileWidth; x < destX; x++)
                {
                    heightMap[y - originY, x - originX] = maxmax[y, x - FileWidth];
                }
            }
            return heightMap;
        }

        public AreaRange GetAreaRange(float lon, float lat, int rad)
        {
            AreaRange areaRange = new AreaRange();

            Point minPt = new Point();
            Point maxPt = new Point();

            minPt.Y = lat - rad /  880000.0f;
            minPt.X = lon - rad / 1110000.0f;
            maxPt.Y = lat + rad /  880000.0f;
            maxPt.X = lon + rad / 1110000.0f;

            areaRange.minPt = minPt;
            areaRange.maxPt = maxPt;

            return areaRange;
        }

        public short[,] LoadHeightmap(int lon, int lat)
        {
            short[,] data = new short[FileWidth, FileHeight];
            using (var file = File.OpenRead("Assets/Resources/heightMap/N" + lat.ToString("000") + "E" + lon.ToString("000") + "_AVE_DSM.raw"))
            using (var reader = new BinaryReader(file))
            {
                for (int y = FileHeight - 1; y >= 0; y--)
                {
                    for (int x = 0; x < FileWidth; x++)
                    {
                        short v = reader.ReadInt16();
                        data[y, x] = v;
                    }
                }
            }
            return data;
        }

        public float Lerp(float a, float b, float ratio)
        {
            return a * (1 - ratio) + ratio * b;
        }
    }
}
