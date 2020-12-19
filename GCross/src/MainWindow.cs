using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace GCross
{
    class MainWindow : Window
    {
        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            DrawingArea puzzleArea = new PuzzleDrawingArea(Puzzle.LoadFromCsv(@"E:\Users\mschw\RiderProjects\GCross\GCross\pattern.csv"));
            Add(puzzleArea);
            ShowAll();

            DeleteEvent += Window_DeleteEvent;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}