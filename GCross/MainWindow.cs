using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace GCross
{
    class MainWindow : Window
    {
#pragma warning disable 649
        // Ignore "never assigned" warnings. These are assigned in builder.Autoconnect() in the constructor.
        [UI] private ModelButton _btnAbout;
        [UI] private ModelButton _btnOpen;
        
        [UI] private FileChooserNative _fileChooserNative;

        [UI] private AboutDialog _aboutDialog;
        [UI] private ButtonBox _aboutButtonBox;
        [UI] private Button _btnAboutClose;
#pragma warning restore 649

        private PuzzleDrawingArea puzzleArea;
        
        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            _btnAboutClose = (Button) _aboutButtonBox.Children[2];
            
            DeleteEvent += WindowOnDeleteEvent;
            _btnOpen.Clicked += BtnOpenOnClicked;
            _btnAbout.Clicked += BtnAboutOnClicked;
            _aboutDialog.DeleteEvent += AboutDialogOnDeleteEvent;
            _btnAboutClose.Clicked += BtnAboutCloseOnClicked;

            puzzleArea = new PuzzleDrawingArea();
            Add(puzzleArea);
            ShowAll();
        }

        private void LoadNewPuzzle(string filepath)
        {
            puzzleArea.Puzzle = Puzzle.LoadFromCsv(filepath);
        }

        private void BtnOpenOnClicked(object sender, EventArgs e)
        {
            /*
            FileChooserDialog fc = new FileChooserDialog("Select the puzzle file to open", this,FileChooserAction.Open,
                "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            if (fc.Run() == (int) ResponseType.Accept)
            {
                LoadNewPuzzle(fc.Filename);
            }
            fc.Dispose();
            */
            if (_fileChooserNative.Run() == (int) ResponseType.Accept)
            {
                LoadNewPuzzle(_fileChooserNative.Filename);
            }
        }


        private void BtnAboutOnClicked(object sender, EventArgs e)
        {
            _aboutDialog.Show();
        }

        private void WindowOnDeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        #region About Dialog Events

        private void BtnAboutCloseOnClicked(object sender, EventArgs e)
        {
            _aboutDialog.Visible = false;
        }

        private void AboutDialogOnDeleteEvent(object o, DeleteEventArgs args)
        {
            _aboutDialog.Visible = false;
            args.RetVal = true;
        }

        #endregion
    }
}