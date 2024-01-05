using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static System.Windows.Forms.AxHost;
using Python.Runtime;
using Path = System.IO.Path;
using System.Collections.Immutable;
using System.Windows.Media;
using System.Windows;
using Application = System.Windows.Forms.Application;
using System.Windows.Controls.Primitives;

namespace WinFormsApp1
{
    internal static class Program
    {

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form() { Text = "Session State Machine States" };
            // Create the WinForms control for displaying the graph
            var viewer = new GViewer();
            // disable editing the graph in the control
            //viewer.LayoutEditingEnabled = false;
            // Create the graph, setting the title
            var graph = new Graph("Session State Machine");
            graph.Attr.LayerDirection = LayerDirection.LR;
            graph.Attr.LayerSeparation = 50;
            graph.Attr.MinNodeHeight = 100;
            graph.Attr.MinNodeWidth = 100;
            graph.Attr.NodeSeparation = 50;
            //graph.Attr.

            graph.Directed = true;
            graph.LayoutAlgorithmSettings.EdgeRoutingSettings.SimpleSelfLoopsForParentEdgesThreshold = 1;
            graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.RectilinearToCenter;
            graph.LayoutAlgorithmSettings.NodeSeparation = 50;
            //graph.LayoutAlgorithmSettings.PackingMethod = PackingMethod.
            graph.LayoutAlgorithmSettings.LiftCrossEdges = true;

            //graph.RootSubgraph;



            viewer.Graph = graph;
            // Enables saving the rendered graph as an image
            viewer.SaveAsImageEnabled = true;
            // disable layout while adding the viewer control, to prevent weird graphical glitches as it does its layout
            form.SuspendLayout();
            // Make the viewer fill the whole form, and add it to the form
            viewer.Dock = DockStyle.Fill;
            form.Controls.Add(viewer);
            // resume laying out the form
            form.ResumeLayout();

            string[] args = Environment.GetCommandLineArgs();
            var filePath = args[1];
            string dir = Path.GetDirectoryName(filePath.Replace("/", @"\"))!;
            
            bool bOpenOption = false;
            if (args.Length > 2)
            {
                bOpenOption = args[2] == "-open";
            }

            var lines = File.ReadLines(filePath);
            var startSymbol = from line in lines
                              where line.Contains("'")
                              select line.Substring(1);

            var transitions = from line in lines
                              where line.Contains("->")
                              select new { From = line.Split("->")[0].Trim(), Input = line.Split(':')[1].Trim(), To = line.Split("->")[1].Split(':')[0].Trim() };

            foreach (var transition in transitions)
            {

                Microsoft.Msagl.Drawing.Edge e = graph.AddEdge(transition.From, transition.Input, transition.To);
                e.Attr.LineWidth = 2;
                e.Attr.Color = Microsoft.Msagl.Drawing.Color.Blue;
                e.Attr.Id = transition.Input;

                e.Label.FontSize = 5;
                e.Label.FontStyle = Microsoft.Msagl.Drawing.FontStyle.Italic;
            }
            foreach (var node in graph.Nodes)
            {
                node.Attr.LineWidth = 3;
                if (node.LabelText == startSymbol.First())
                {
                    node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Orange;
                }
                else
                {
                    node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightGray;
                }
                node.Label.FontSize = 14;
                node.Label.FontStyle = Microsoft.Msagl.Drawing.FontStyle.Bold;
            }
            // Assign the graph to the viewer
            viewer.Graph = graph;
            // Enables saving the rendered graph as an image
            viewer.SaveAsImageEnabled = true;
            // disable layout while adding the viewer control, to prevent weird graphical glitches as it does its layout
            form.SuspendLayout();
            // Make the viewer fill the whole form, and add it to the form
            viewer.Dock = DockStyle.Fill;
            form.Controls.Add(viewer);
            // resume laying out the form
            form.ResumeLayout();
            // launch the form

            
            if(bOpenOption)
            {
                System.Windows.Forms.Application.Run(form);
            }
            //export img...
            else
            {
                graph.Write(Path.Combine(dir, "gen.MsGraph.msagl"));


                int w, h;
                int ImageScale = 5;
                w = (int)Math.Ceiling(viewer.Graph.Width * ImageScale);
                h = (int)Math.Ceiling(viewer.Graph.Height * ImageScale);


                Bitmap bitmap = null;

                bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    //fill the whole image
                    graphics.FillRectangle(new SolidBrush(Draw.MsaglColorToDrawingColor(viewer.Graph.Attr.BackgroundColor)),
                                           new RectangleF(0, 0, w, h));

                    //calculate the transform
                    double s = ImageScale;
                    Graph g = viewer.Graph;
                    double x = 0.5 * w - s * (g.Left + 0.5 * g.Width);
                    double y = 0.5 * h + s * (g.Bottom + 0.5 * g.Height);

                    graphics.Transform = new System.Drawing.Drawing2D.Matrix((float)s, 0, 0, (float)-s, (float)x, (float)y);
                    Draw.DrawPrecalculatedLayoutObject(graphics, viewer.ViewerGraph);
                }


                if (bitmap != null)
                    bitmap.Save(Path.Combine(dir, "gen.MsaglGraph.png"));

                string modulepath = dir.Substring(0, dir.LastIndexOf(@"StateMachine\"));
                modulepath = Path.Combine(modulepath, @"StateMachine\MsaglGenerator\modules");

                string dllPath = Path.Combine(dir.Substring(0, dir.LastIndexOf(@"StateMachine\")), "StateMachine", "MsaglGenerator", "dll", "python312.dll");
                Runtime.PythonDLL = dllPath;
                PythonEngine.Initialize();
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.insert(0, modulepath);
                    dynamic pu = Py.Import("plantuml");
                    dynamic url = "http://www.plantuml.com/plantuml/img/";
                    dynamic pl = pu.PlantUML(url);
                    string param = string.Format(@"'{0}', outfile = 'gen.plantuml.png', directory = '{1}'", filePath, dir);
                    //pl.processes_file(param);
                    dynamic script = string.Format(@"
import sys
sys.path.insert(0, '.\\modules');
import plantuml

pl = plantuml.PlantUML('http://www.plantuml.com/plantuml/img/')
pl.processes_file({0})", param);
                    PythonEngine.RunString(script);
                }
                PythonEngine.Shutdown();
            }
        }
    }
}