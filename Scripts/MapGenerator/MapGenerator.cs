using Godot;
using System;
using System.Text;

public class MapGenerator : Node
{
    private Random _random;
    private int _seed;
    private bool _shouldEmptySeed = false;

    private NodePath _lineEdit = "../ControlPanel/VBoxContainer/SeedInput";
    private LineEdit _seedInput = null;

    public override void _Ready()
    {
        try
        {
            _seedInput = (LineEdit)GetNode(_lineEdit);
        }
        catch (Exception ex)
        {
            GD.Print($"Exception: {ex.Message}");
        }
    }

    private void CreateTestMap()
    {
        const int size = 20;

        var map = new int[size, size];

        GD.Print("Started.");

        GD.Print($"Seed retrieved: {_seed}");

        var sb = new StringBuilder();

        for (int i = 0; i < size; ++i)
        {
            for (int j = 0; j < size; ++j)
            {
                map[i, j] = _random.Next(0, 2);
                if (map[i, j] == 0)
                {
                    sb.Append('o');
                }
                else
                {
                    sb.Append('x');
                }
            }
            sb.Append('\n');
        }

        GD.Print("MAP:");
        GD.Print(sb.ToString());

        GD.Print("Finished.");
    }

    private void _on_GenerateMapButton_button_up()
    {
        try
        {
            if (_shouldEmptySeed)
            {
                _seedInput.Clear();
            }

            if (!_seedInput.Text.Empty())
            {
                _seed = _seedInput.Text.GetHashCode();
                _random = new Random(_seed);
                _shouldEmptySeed = false;
            }
            else
            {
                _seed = (int)DateTime.UtcNow.Ticks;
                _random = new Random(_seed);
                _seedInput.Text = _seed.ToString();
                _shouldEmptySeed = true;
            }
        }
        catch (Exception ex)
        {
            GD.Print($"Exception in signal: {ex.Message}\n{ex.StackTrace}");
        }

        CreateTestMap();
    }
}
