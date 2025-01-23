using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System;
using System.Globalization;
using System.Text;

namespace ThreeDeeBear.Models.Ply
{
    public class PlyResult
    {
        public List<Vector3> Vertices;
        public List<int> Triangles;
        public List<Color> Colors;
		public List<Vector2> Uvs;

        public PlyResult(List<Vector3> vertices, List<int> triangles, List<Color> colors, List<Vector2> uvs)
        {
            Vertices = vertices;
            Triangles = triangles;
            Colors = colors;
			Uvs = uvs;
        }
    }
    public static class PlyHandler
    {
        #region Ascii

        private static PlyResult ParseAscii(List<string> plyFile, PlyHeader header)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
			var uvs = new List<Vector2>();
            var headerEndIndex = plyFile.IndexOf("end_header");
            var vertexStartIndex = headerEndIndex + 1;
            var faceStartIndex = vertexStartIndex + header.VertexCount;
            plyFile.GetRange(vertexStartIndex, header.VertexCount).ForEach(vertex =>
            {
                var xyzrgb = vertex.Split(' ');
                vertices.Add(ParseVertex(xyzrgb, header));
                colors.Add(ParseColor(xyzrgb, header));
				
				var st = vertex.Split(' ');
                uvs.Add(ParseUvs(st, header));
            });

            plyFile.GetRange(faceStartIndex, header.FaceCount).ForEach(face =>
            {
                triangles.AddRange(GetTriangles(face, header));
            });
			
            return new PlyResult(vertices, triangles, colors, uvs);
        }
        private static Vector3 ParseVertex(string[] xyzrgb, PlyHeader header)
        {
            decimal dx, dy, dz;
            decimal.TryParse(xyzrgb[header.XIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out dx);
            decimal.TryParse(xyzrgb[header.YIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out dy);
            decimal.TryParse(xyzrgb[header.ZIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out dz);
            return new Vector3((float)dx, (float)dy, (float)dz);
        }

        private static Color ParseColor(string[] xyzrgb, PlyHeader header)
        {
            int r = 255;
            int g = 255;
            int b = 255;
            int a = 255;
            if (header.RedIndex.HasValue)
                int.TryParse(xyzrgb[header.RedIndex.Value], NumberStyles.Integer, CultureInfo.InvariantCulture, out r);
            if (header.GreenIndex.HasValue)
                int.TryParse(xyzrgb[header.GreenIndex.Value], NumberStyles.Integer, CultureInfo.InvariantCulture, out g);
            if (header.BlueIndex.HasValue)
                int.TryParse(xyzrgb[header.BlueIndex.Value], NumberStyles.Integer, CultureInfo.InvariantCulture, out b);
            if (header.AlphaIndex.HasValue)
                int.TryParse(xyzrgb[header.AlphaIndex.Value], NumberStyles.Integer, CultureInfo.InvariantCulture, out a);
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }
		
		private static Vector2 ParseUvs(string[] st, PlyHeader header)
        {
            decimal ds, dt;
            decimal.TryParse(st[header.SIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out ds);
            decimal.TryParse(st[header.TIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out dt);
            return new Vector2((float)ds, (float)dt);
        }

        private static List<int> GetTriangles(string faceVertexList, PlyHeader header)
        {
            switch (header.FaceParseMode)
            {
                case PlyFaceParseMode.VertexCountVertexIndex:
                    var split = faceVertexList.Split(' ');
                    var count = Convert.ToInt32(split.First());
                    switch (count)
                    {
                        case 3: // triangle
                            return split.ToList().GetRange(1, 3).Select(x => Convert.ToInt32(x)).ToList();
                        case 4: // face
                            var triangles = new List<int>();
                            var indices = split.ToList().GetRange(1, 4).Select(x => Convert.ToInt32(x)).ToList();
                            triangles.AddRange(QuadToTriangles(indices));
                            return triangles;
                        default:
                            Debug.LogWarning("Warning: Found a face with more than 4 vertices, skipping...");
                            return new List<int>();
                    }
                default:
                    Debug.LogWarning("Ply GetTriangles: Unknown parse mode");
                    return new List<int>();
            }
        }
		
        #endregion

        private static int GetByteCountPerVertex(PlyHeader header)
        {
            int bpvc = 4; // bytes per vertex component
            int bpcc = 1; // bytes per color component
            // todo: support other types than just float for vertex components and byte for color components
            int r = header.RedIndex.HasValue ? bpcc : 0;
            int g = header.GreenIndex.HasValue ? bpcc : 0;
            int b = header.BlueIndex.HasValue ? bpcc : 0;
            int a = header.AlphaIndex.HasValue ? bpcc : 0;
            return (3 * bpvc + r + g + b + a);
        }

        private static List<int> QuadToTriangles(List<int> quad)
        {
            return new List<int>() { quad[0], quad[1], quad[2], quad[2], quad[3], quad[0] };
        }


        public static PlyResult GetVerticesAndTriangles(string path)
        {
            List<string> header = File.ReadLines(path).TakeUntilIncluding(x => x == "end_header").ToList();
            var headerParsed = new PlyHeader(header);
            if (headerParsed.Format == PlyFormat.Ascii)
            {
                return ParseAscii(File.ReadAllLines(path).ToList(), headerParsed);
            }
            else // todo: support BinaryBigEndian
            {
                return null;
            }
        }

    }
}