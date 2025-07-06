using UnityEngine;
using System;
using System.Collections.Generic;


namespace Common.Unity.Drawing
{

    public class NormalRenderer : BaseRenderer
    {

        private List<Vector4> _mNormals = new List<Vector4>();

        public NormalRenderer()
        {

        }

        public NormalRenderer(DRAW_ORIENTATION orientation)
        {
            Orientation = orientation;
        }

        public override void Clear()
        {
            base.Clear();
            _mNormals.Clear();
        }

        public float Length = 1;

        public void Load(IList<Vector2> vertices)
        {
            foreach (var v in vertices)
            {
                var n = v.normalized;
                if (Orientation == DRAW_ORIENTATION.XY)
                {
                    Vertices.Add(v);
                    _mNormals.Add(n);
                }
                else if (Orientation == DRAW_ORIENTATION.XZ)
                {
                    Vertices.Add(new Vector4(v.x, 0, v.y, 1));
                    _mNormals.Add(new Vector4(n.x, 0, n.y, 1));
                }
                    
                Colors.Add(DefaultColor);
            }
        }

        public void Load(IList<Vector2> vertices, IList<Vector2> normals)
        {
            foreach (var v in vertices)
            {
                if (Orientation == DRAW_ORIENTATION.XY)
                    Vertices.Add(v);
                else if (Orientation == DRAW_ORIENTATION.XZ)
                    Vertices.Add(new Vector4(v.x, 0, v.y, 1));

                Colors.Add(DefaultColor);
            }

            foreach (var n in normals)
            {
                if (Orientation == DRAW_ORIENTATION.XY)
                    _mNormals.Add(n);
                else if (Orientation == DRAW_ORIENTATION.XZ)
                    _mNormals.Add(new Vector4(n.x, 0, n.y, 1));
            }
        }

        public void Load(IList<Vector2> vertices, IList<Vector2> normals, Color col)
        {
            foreach (var v in vertices)
            {
                if (Orientation == DRAW_ORIENTATION.XY)
                    Vertices.Add(v);
                else if (Orientation == DRAW_ORIENTATION.XZ)
                    Vertices.Add(new Vector4(v.x, 0, v.y, 1));

                Colors.Add(col);
            }

            foreach (var n in normals)
            {
                if (Orientation == DRAW_ORIENTATION.XY)
                    _mNormals.Add(n);
                else if (Orientation == DRAW_ORIENTATION.XZ)
                    _mNormals.Add(new Vector4(n.x, 0, n.y, 1));
            }
        }

        public void Load(IList<Vector3> vertices)
        {
            foreach (var v in vertices)
            {
                var n = v.normalized;
                Vertices.Add(v);
                _mNormals.Add(n);
                Colors.Add(DefaultColor);
            }
        }

        public void Load(IList<Vector3> vertices, IList<Vector3> normals)
        {
            foreach (var v in vertices)
            {
                Vertices.Add(v);
                Colors.Add(DefaultColor);
            }

            foreach (var n in normals)
                _mNormals.Add(n);
        }

        public void Load(IList<Vector3> vertices, IList<Vector3> normals, Color col)
        {
            foreach (var v in vertices)
            {
                Vertices.Add(v);
                Colors.Add(col);
            }

            foreach (var n in normals)
                _mNormals.Add(n);
        }

        protected override void OnDraw(Camera camera, Matrix4x4 localToWorld)
        {
            GL.PushMatrix();

            GL.LoadIdentity();
            GL.modelview = camera.worldToCameraMatrix * localToWorld;
            GL.LoadProjectionMatrix(camera.projectionMatrix);

            Material.SetPass(0);
            GL.Begin(GL.LINES);

            int vertexCount = Vertices.Count;
            for (int i = 0; i < vertexCount; i++)
            {
                GL.Color(Colors[i]);
                GL.Vertex(Vertices[i]);
                GL.Vertex(Vertices[i] + _mNormals[i] * Length);
            }

            GL.End();

            GL.PopMatrix();
        }

    }

}