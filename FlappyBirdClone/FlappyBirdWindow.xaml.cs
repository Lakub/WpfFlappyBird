using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        Stopwatch sw;
        Size originalSize;
        static public Canvas GameCanvas;
        static public List<GameElement> gameElements;
        public FlappyBirdWindow()
        {
            InitializeComponent();
            gameElements = new();
            originalSize = new Size(this.Width, Height);
            GameCanvas = GameScreen;
            Time.deltaSeconds=0;
            sw = new();
            sw.Start();
            new Bird(BirdSprite);
            CompositionTarget.Rendering += Test;
            SizeChanged += ResizeCanvas;
            KeyDown += AlertBird;
        }
        public void AlertBird(object o, KeyEventArgs e)
        {
            (gameElements[0] as Bird).KeyDown(e.Key);
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
        public void Test(object sender,EventArgs e)
        {
            Time.deltaSeconds = sw.Elapsed.TotalSeconds;
            sw.Restart();
            foreach (var element in gameElements)
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
            FlappyBirdWindow.gameElements.Add(this);
            this.uiElement = uiElement;
        }
        protected void Move(double x, double y)
        {
            Canvas.SetLeft(uiElement, Canvas.GetLeft(uiElement) + x);
            Canvas.SetTop(uiElement, Canvas.GetTop(uiElement) - y);
        }
        abstract public void Update();

        public abstract GameElement DeepCopy();
        public static void CreateCopy(GameElement gameElement)
        {
            var elementClone = gameElement.DeepCopy();
            FlappyBirdWindow.GameCanvas.Children.Add(gameElement.uiElement);
        }
    }
    public class Bird : GameElement
    {
        bool jumpPressed;
        double verticalAcceleration;
        public Bird(UIElement uiElement) : base(uiElement)
        {
        }
        public override void Update()
        {
            if (jumpPressed)
            {
                verticalAcceleration += 15;
            }
            else
            {

            }
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
            string saved = XamlWriter.Save(uiElement);
            var element = XamlReader.Parse(saved);
            return new Bird((UIElement)element);
        }
    }
}