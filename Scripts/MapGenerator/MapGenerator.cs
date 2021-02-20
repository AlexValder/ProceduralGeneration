using Godot;
using System;

public class MapGenerator : Node
{
    private bool _generated = false;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey e)
        {
            if (e.Scancode == (uint)KeyList.M && e.IsPressed())
            {
                CreateTestMap();
            }
        }
    }

    private void CreateTestMap()
    {
        if (_generated) return;
        _generated = true;

        GD.Print("Started...");
        var spatialScene = GetNode("/root/Spatial/");

        var st = new SurfaceTool();
        
        st.Begin(Mesh.PrimitiveType.TriangleFan);

        st.AddVertex(new Vector3(-1, 0, 1));
        st.AddVertex(new Vector3(-1, 0, -1));
        st.AddVertex(new Vector3(1, 0, -1));

        var mi = new MeshInstance
        {
            Name = "MAP",
            Mesh = st.Commit()
        };

        spatialScene.AddChild(mi);
        GD.Print("Finished...");
        GetNode("/root/").PrintTreePretty();
    }
}
