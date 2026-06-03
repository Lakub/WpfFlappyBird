using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace FlappyBirdClone
{
    public class Time
    {
        static double dS;
        static public double deltaSeconds
        {
            get
            {
                return dS* timeScale;
            }
            set
            {
                dS = value;
            }
        }
        static public double timeScale = 1;
        
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FlappyBirdWindow : Window
    {
        public static Random random;
        Stopwatch sw;
        Size originalSize;
        static public Canvas GameCanvas;
        public Bird bird;
        static public Pipe templatePipe;
        static public List<Pipe> pipes;
        static public int points;
        public FlappyBirdWindow()
        {
            InitializeComponent();
            random = new();
            pipes = new();
            originalSize = new Size(this.Width, Height);
            GameCanvas = GameScreen;
            Time.deltaSeconds=0;
            sw = new();
            sw.Start();
            bird = new Bird(BirdSprite);
            templatePipe = new Pipe(PipeSprite,false);
            templatePipe.enabled = false;
            CompositionTarget.Rendering += UpdateGameElements;
            SizeChanged += ResizeCanvas;
            KeyDown += AlertBird;
        }
        public void AlertBird(object o, KeyEventArgs e)
        {
            bird.KeyDown(e.Key);
        }
        public void ResizeCanvas(object sender, SizeChangedEventArgs e)
        {
            var scale = (GameScreen.LayoutTransform as ScaleTransform);
            var diffWidth = e.NewSize.Height / originalSize.Height;
            var diffHeight = e.NewSize.Width / originalSize.Width;
            double scaleVal = 0;
            if (diffWidth > diffHeight)
            {
                scaleVal = diffHeight;
            }
            else
            {
                scaleVal = diffWidth;
            }
            scale.ScaleX = scaleVal;
            scale.ScaleY = scaleVal;
        }
        void CreatePipe()
        {
            var pipe = pipes.Find(e => !e.enabled);
            if (pipe == null)
                templatePipe.DeepCopy();
            else
                pipe.ResetPosition();
        }
        double counter=0;
        public void UpdateGameElements(object sender,EventArgs e)
        {
            Time.deltaSeconds = sw.Elapsed.TotalSeconds;
            sw.Restart();
            counter += Time.deltaSeconds;
            if (counter >= 3.8)
            {
                counter = 0;
                CreatePipe();
            }
            bird.Update();
            foreach (var element in pipes)
            {
                element.Update();
            }
        }
    }

    public abstract class GameElement
    {
        public UIElement uiElement
        {
            get;
            protected set;
        }
        protected GameElement(UIElement uiElement)
        {
            this.uiElement = uiElement;
        }
        protected void Move(double x, double y)
        {
            Canvas.SetLeft(uiElement, Canvas.GetLeft(uiElement) + x);
            Canvas.SetTop(uiElement, Canvas.GetTop(uiElement) - y);
        }
        abstract public void Update();
        protected UIElement CopyElement()
        {
            string saved = XamlWriter.Save(uiElement);
            string pattern = "Name\\s*=\\s*\"([a-zA-Z]*|\\s*|\\d*)\"\\s";
            var element = XamlReader.Parse(Regex.Replace(saved, pattern, String.Empty));
            FlappyBirdWindow.GameCanvas.Children.Add(element as UIElement);
            return element as UIElement;
        }
        
        public abstract GameElement DeepCopy();
        public static void CreateCopy(GameElement gameElement)
        {
            var elementClone = gameElement.DeepCopy();
            FlappyBirdWindow.GameCanvas.Children.Add(gameElement.uiElement);
        }
    }

    public class Pipe : GameElement
    {
        static int minY = 131;
        static int maxY = 426;
        static int minYDistance = -701;
        static int maxYDistance = -615;
        static int startingX = 805;
        static int endingX = -105;

        Pipe topPipe;
        public bool enabled
        {
            get
            {
                return uiElement.Visibility == Visibility.Visible;
            }
            set
            {
                if (value)
                    uiElement.Visibility = Visibility.Visible;
                else
                    uiElement.Visibility = Visibility.Collapsed;
            }
        }
        public Pipe(UIElement uiElement, bool enabled=true) : base(uiElement)
        {
            if(enabled){
                FlappyBirdWindow.pipes.Add(this);
                topPipe = CreateTopPipe();
                ResetPosition();
            }
        }
        public Pipe(UIElement uiElement, int dum) : base(uiElement) { }

        public void ResetPosition()
        {
            enabled = true;
            Canvas.SetLeft(uiElement, startingX);
            var myY = FlappyBirdWindow.random.Next(minY, maxY + 1);
            Canvas.SetTop(uiElement, myY);
            topPipe.ResetTopPosition(myY);
        }
        public void ResetTopPosition(int botY)
        {
            enabled = true;
            Canvas.SetLeft(uiElement, startingX);
            var pos = botY + FlappyBirdWindow.random.Next(minYDistance, maxYDistance + 1);
            Trace.WriteLine(pos);
            Canvas.SetTop(uiElement, pos);
        }

        public Pipe CreateTopPipe()
        {
            return new Pipe(CopyElement(),0);
        }

        public override GameElement DeepCopy()
        {
            return new Pipe(CopyElement());
        }

        public override void Update()
        {
            if(!enabled) return;
            if(topPipe!=null)
                topPipe.Update();
            Move(-140*Time.deltaSeconds, 0);
            if (Canvas.GetLeft(uiElement) <= endingX)
                enabled = false;
        }
    }
    public class Bird : GameElement
    {
        bool jumpPressed;
        double verticalAcceleration;
        RotateTransform rotateTransform;
        public Bird(UIElement uiElement) : base(uiElement)
        {
            rotateTransform = uiElement.RenderTransform as RotateTransform;
        }
        public override void Update()
        {
            if (jumpPressed)
            {
                verticalAcceleration = 200;
            }
            else
            {
                verticalAcceleration -= 250*Time.deltaSeconds;
            }

            rotateTransform.Angle += -verticalAcceleration * 5 * Time.deltaSeconds;
            rotateTransform.Angle=Math.Clamp(rotateTransform.Angle, -30, 30);
            Move(0, verticalAcceleration * Time.deltaSeconds);
            jumpPressed = false;
        }
        public void KeyDown(Key key)
        {
            if (key == Key.Up || key == Key.Space || key == Key.W)
                jumpPressed = true;
        }
        public override GameElement DeepCopy()
        {
            
            return new Bird(CopyElement());
        }
    }
}