using System;
using System.Collections.Generic;
using System.IO;
using Saraff.Tiff;
using Saraff.Tiff.Core;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace TerrainPreprocessor
{
    class TiffIO
    {
        private TiffReader Reader { get; set; }
        private Dictionary<TiffTags, Collection<object>> Dictionary;
        private List<byte[]> ValuesList { get; set; }

        public TiffIO()
        {
            Dictionary = new Dictionary<TiffTags, Collection<object>>();
            ValuesList = new List<byte[]>();
        }

        public ushort ImageHeight { get; set; }
        public ushort ImageWidth { get; set; }

        #region Get Heights
        public int[,] GetHeightsInt(int index)
        {
            int[,] values = new int[ImageHeight, ImageWidth];
            Buffer.BlockCopy(ValuesList[index], 0, values, 0, ValuesList[index].Length);
            return values;
        }
        
        public uint[,] GetHeightsUint(int index)
        {
            uint[,] values = new uint[ImageHeight, ImageWidth];
            Buffer.BlockCopy(ValuesList[index], 0, values, 0, ValuesList[index].Length);
            return values;
        }

        public short[,] GetHeightsShort(int index)
        {
            short[,] values = new short[ImageHeight, ImageWidth];
            Buffer.BlockCopy(ValuesList[index], 0, values, 0, ValuesList[index].Length);
            return values;
        }

        public ushort[,] GetHeightsUshort(int index)
        {
            ushort[,] values = new ushort[ImageHeight, ImageWidth];
            Buffer.BlockCopy(ValuesList[index], 0, values, 0, ValuesList[index].Length);
            return values;
        }

        public float[,] GetHeightsFloat(int index)
        {
            float[,] values = new float[ImageHeight, ImageWidth];
            Buffer.BlockCopy(ValuesList[index], 0, values, 0, ValuesList[index].Length);
            return values;
        }

        public double[,] GetHeightsDouble(int index)
        {
            double[,] values = new double[ImageHeight, ImageWidth];
            Buffer.BlockCopy(ValuesList[index], 0, values, 0, ValuesList[index].Length);
            return values;
        }
        #endregion

        #region Set Heights
        public void SetHeightsInt(int[,] values, int index)
        {
            ValuesList[index] = new byte[values.Length * sizeof(int)];
            Buffer.BlockCopy(values, 0, ValuesList[index], 0, values.Length * sizeof(int));
        }

        public void SetHeightsUint(uint[,] values, int index)
        {
            ValuesList[index] = new byte[values.Length * sizeof(uint)];
            Buffer.BlockCopy(values, 0, ValuesList[index], 0, values.Length * sizeof(uint));
        }

        public void SetHeightsShort(short[,] values, int index)
        {
            ValuesList[index] = new byte[values.Length * sizeof(short)];
            Buffer.BlockCopy(values, 0, ValuesList[index], 0, values.Length * sizeof(short));
        }

        public void SetHeightsUshort(ushort[,] values, int index)
        {
            ValuesList[index] = new byte[values.Length * sizeof(ushort)];
            Buffer.BlockCopy(values, 0, ValuesList[index], 0, values.Length * sizeof(ushort));
        }

        public void SetHeightsFloat(float[,] values, int index)
        {
            ValuesList[index] = new byte[values.Length * sizeof(float)];
            Buffer.BlockCopy(values, 0, ValuesList[index], 0, values.Length * sizeof(float));
        }

        public void SetHeightsDouble(double[,] values, int index)
        {
            ValuesList[index] = new byte[values.Length * sizeof(double)];
            Buffer.BlockCopy(values, 0, ValuesList[index], 0, values.Length * sizeof(double));
        }
        #endregion

        public bool LoadTiff(string Filename, out Exception errorMsg)
        {
            errorMsg = new Exception();
            try
            {
                using (var stream = File.Open(Filename, FileMode.Open))
                {
                    var reader = TiffReader.Create(stream);

                    reader.ReadHeader(); // Read Header

                    // Read Image File Directories
                    for (var count = reader.ReadImageFileDirectory(); count != 0; count = reader.ReadImageFileDirectory())
                    {
                        Dictionary.Clear();

                        // Read Tags
                        for (ITag tag = reader.ReadTag(); tag != null; tag = reader.ReadTag())
                        {
                            Dictionary.Add(tag.TagId, new Collection<object>());
                            switch (tag.TagId)
                            {
                                case TiffTags.StripOffsets:
                                    // Read Values of Tag
                                    for (object value = reader.ReadHandle(); value != null; value = reader.ReadHandle())
                                    {
                                        Dictionary[tag.TagId].Add(value);
                                    }
                                    break;
                                default:
                                    // Read Values of Tag
                                    for (object value = reader.ReadValue(); value != null; value = reader.ReadValue())
                                    {
                                        Dictionary[tag.TagId].Add(value);
                                    }
                                    break;
                            }
                        }

                        // Read Strips
                        for (int i = 0; i < Dictionary[TiffTags.StripOffsets].Count; i++)
                        {
                            ValuesList.Clear();
                            var data = reader.ReadData((TiffHandle)Dictionary[TiffTags.StripOffsets][i], Convert.ToInt64(Dictionary[TiffTags.StripByteCounts][i]));
                            ValuesList.Add(data);
                        }

                        ImageHeight = (ushort)Dictionary[TiffTags.ImageLength][0];
                        ImageWidth = (ushort)Dictionary[TiffTags.ImageWidth][0];

                    }
                }
            }
            catch(Exception e)
            {
                ValuesList.Add(new byte[0]);
                errorMsg = e;
                return false;
            }
            return true;
        }

        public bool SaveTiff(string Filename)
        {
            try { 
                using (var stream = File.Create(Filename))
                {
                    var writer = TiffWriter.Create(stream);
                    var handle = writer.WriteHeader(); // Write Header
                    
                    var bitPerSample = (ushort)Dictionary[TiffTags.BitsPerSample][0];

                    var strips = new TiffHandle[ValuesList.Count];
                    var stripByteCounts = new uint[ValuesList.Count];
                    for (int i = 0; i < ValuesList.Count; i++)
                    {
                        var _buf = new byte[ValuesList[i].Length];
                        Buffer.BlockCopy(ValuesList[i], 0, _buf, 0, ValuesList[i].Length);

                        // Generating data ...
                        strips[i] = writer.WriteData(_buf); // Write Strip
                        stripByteCounts[i] = (uint)_buf.Length;
                    }

                    // Write Image File Directory
                    handle = writer.WriteImageFileDirectory(handle, new Collection<ITag> {
                        Tag<uint>.Create(TiffTags.ImageWidth, ImageWidth),
                        Tag<uint>.Create(TiffTags.ImageLength, ImageHeight),
                        Tag<ushort>.Create(TiffTags.BitsPerSample, bitPerSample),
                        Tag<TiffCompression>.Create(TiffTags.Compression, TiffCompression.NONE),
                        Tag<TiffPhotoMetric>.Create(TiffTags.PhotometricInterpretation, TiffPhotoMetric.BlackIsZero),
                        Tag<TiffHandle>.Create(TiffTags.StripOffsets, strips),
                        Tag<ushort>.Create(TiffTags.SamplesPerPixel, 1),
                        Tag<uint>.Create(TiffTags.RowsPerStrip, ImageWidth),
                        Tag<uint>.Create(TiffTags.StripByteCounts, stripByteCounts),
                        Tag<char>.Create(TiffTags.SampleFormat, "INT".ToCharArray()),
                        Tag<double>.Create((TiffTags)33550, 0.000277777777778001, 0.000277777777778001, 0 ),
                        Tag<double>.Create((TiffTags)33922, 0, 0, 0, 0, 0, 0 ),
                        Tag<double>.Create((TiffTags)34735, 1, 1, 0, 7, 1024, 0, 1, 2, 1025, 0, 1, 1, 2048, 
                        0, 1, 4326, 2049, 34737, 7, 0, 2054, 0, 1, 9102, 2057, 34736, 1, 1, 2059, 34736, 1, 0),
                        Tag<double>.Create((TiffTags)298.257223563, 6378137),
                        Tag<char>.Create((TiffTags)34737, "WGS 84|".ToCharArray()),
                        Tag<char>.Create(TiffTags.Software, "BirdEye Software".ToCharArray()),
                        Tag<char>.Create(TiffTags.Copyright, "(c) Alos Gdem".ToCharArray())
                    });
                }
                return true;
            } 
            catch (Exception)
            {
                return false;
            }
        }
    }
}
