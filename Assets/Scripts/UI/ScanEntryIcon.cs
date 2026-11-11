using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// Text-free, resolution-independent scan/start control.
    [RequireComponent(typeof(CanvasRenderer))]
    public class ScanEntryIcon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .47f;
            Color fill = new Color(.015f,.055f,.10f,.94f) * color;
            Color rim = new Color(.3f,.88f,1f,1f) * color;
            const int segments = 64;
            for (int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments, b=(i+1)*Mathf.PI*2/segments;
                Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                Vector2 q=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                Triangle(mesh,center,center+p*radius,center+q*radius,fill);
                Quad(mesh,center+p*(radius-2),center+p*radius,center+q*radius,center+q*(radius-2),rim);
            }
            float span=radius*.49f, arm=span*.43f, thickness=radius*.045f;
            for(int x=-1;x<=1;x+=2)
                for(int y=-1;y<=1;y+=2)
                {
                    Vector2 corner=center+new Vector2(x*span,y*span);
                    Bar(mesh,corner,corner-new Vector2(x*arm,0),thickness,rim);
                    Bar(mesh,corner,corner-new Vector2(0,y*arm),thickness,rim);
                }
            Triangle(mesh,center+new Vector2(-radius*.13f,-radius*.23f),
                center+new Vector2(-radius*.13f,radius*.23f),center+new Vector2(radius*.26f,0),color);
        }
        private static void Bar(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            Quad(mesh,a-normal,a+normal,b+normal,b-normal,tint);
        }
        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int index=mesh.currentVertCount;
            mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);mesh.AddVert(c,tint,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);
        }
        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int index=mesh.currentVertCount;
            mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);mesh.AddVert(c,tint,Vector2.zero);mesh.AddVert(d,tint,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
        }
    }
}
