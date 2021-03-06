﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ID_Card_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Windows user application data directory
        /// </summary>
        string appdata;
        /// <summary>
        /// Subdirectory of user application data for ID Card Maker
        /// </summary>
        string appdir;

        CardPreview cardPreviewerObverse = new CardPreview();
        ReversePreview cardPreviewerReverse = new ReversePreview();



        /// <summary>
        /// Constructor for <code>MainWindow</code>
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appdir = System.IO.Path.Combine(appdata, @"Dan Leonard\ID-Card-Maker");

            cardPreviewViewbox.Child = cardPreviewerObverse;
            cardPreviewReverseViewbox.Child = cardPreviewerReverse;

            foreach (CardPreview.Design design in cardPreviewerObverse.Designs)
            {
                // add designs to radio buttons
                RadioButtonDesign picker = new RadioButtonDesign();
                picker.Content = design.canonical_title;
                picker.Design = design;
                picker.Margin = new Thickness(10);
                RoutedEventArgs args = new RoutedEventArgs();
                picker.Checked += new RoutedEventHandler(cardPreviewerObverse.SetDesign);

                CardDesignChoosers.Children.Add(picker);



                // add settings options to menu bar
                MenuItem menuItem = new MenuItem
                {
                    Header = "_" + design.canonical_title,
                    IsCheckable = true,
                };
                MenuItemExtensions.SetGroupName(menuItem, "Settings_Default");

                MenuItem_Settings_Default.Items.Add(menuItem);
            }

            // load from user setting
            int setting = Properties.Settings.Default.Design;
            (CardDesignChoosers.Children[setting] as RadioButtonDesign).IsChecked = true;
            (MenuItem_Settings_Default.Items[setting] as MenuItem).IsChecked = true;
        }

        private void InvokePrintReverse(object sender, RoutedEventArgs e)
        {
            Print(cardPreviewerReverse, 1);
        }


        /// <summary>
        /// Open system print dialog and send it the card preview
        /// </summary>
        private void InvokePrintObverse(object sender, RoutedEventArgs e)
        {
            /*
            // Create a print dialog object
            PrintDialog dialog = new PrintDialog();
            dialog.UserPageRangeEnabled = true;

            if (dialog.ShowDialog() == true)
            {
                // Print the card design
                //dialog.PrintVisual(cardPreviewer, "ID Card");
                PrintHelper.ShowPrintPreview(PrintHelper.GetFixedDocument(cardPreviewer, dialog));
            }
            */

            ArchiveData();
            if (cardPreviewerObverse.Footer.Children.Count != 0 )
            {
                try
                {
                    // will throw NullReferenceException if not visitor pass
                    ((cardPreviewerObverse.Footer.Children[0] as Panel).Children[0] as Label).Content =
                    "Admitted " + DateTime.Now.ToShortDateString() + " at " + DateTime.Now.ToShortTimeString();

                    Print(cardPreviewerObverse, 1);

                    ((cardPreviewerObverse.Footer.Children[0] as Panel).Children[0] as Label).Content = "Print Date";
                }
                catch (NullReferenceException ex)
                {
                    // is not visitor pass
#if DEBUG
                    MessageBox.Show(ex.Message + "\r\nMay be non-visitor pass", ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                    Print(cardPreviewerObverse, 1);
                }
                catch (Exception ex)
                {
#if DEBUG
                    MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                    Print(cardPreviewerObverse, 1);
                }
            }
            else
            {
                Print(cardPreviewerObverse, 1);
            }
        }

        /// <summary>
        /// Method for safe printing all on one page
        /// </summary>
        /// <param name="v">Visual to be printed</param>
        /// <param name="page">Page to be printed</param>
        private void Print(Visual v, int page)
        {

            System.Windows.FrameworkElement e = v as System.Windows.FrameworkElement;
            if (e == null)
                return;

            PrintDialog pd = new PrintDialog();

            // mandate specific page
            pd.UserPageRangeEnabled = false;
            pd.PageRange = new PageRange(page);

            // set to id maker
            try
            {
                pd.PrintQueue = new System.Printing.PrintQueue(new System.Printing.PrintServer(), Properties.Settings.Default.PrinterName);
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show("Exception caught during printer choosing.\r\n\r\n" +
                                ex.Message +
                                "\r\nCurrent printer in Properties.Resources: " +
                                Properties.Settings.Default.PrinterName + ".",
                                "Exception",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
#else
                MessageBoxResult exRes = MessageBox.Show("Ensure your " +
                                                            Properties.Settings.Default.PrinterName +
                                                            " is plugged in and ready." +
                                                            "\r\n\r\nPrint to another printer?",
                                                            "Error printing.",
                                                            MessageBoxButton.OKCancel,
                                                            MessageBoxImage.Error,
                                                            MessageBoxResult.Cancel);
                switch (exRes)
                {
                    case MessageBoxResult.OK:
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
#endif
            }

            if (pd.ShowDialog().Value)
            {
                //store original scale
                Transform originalScale = e.LayoutTransform;
                //get selected printer capabilities
                System.Printing.PrintCapabilities capabilities = pd.PrintQueue.GetPrintCapabilities(pd.PrintTicket);

                //get scale of the print wrt to screen of WPF visual
                double scale = Math.Min(capabilities.PageImageableArea.ExtentWidth / e.ActualWidth, capabilities.PageImageableArea.ExtentHeight /
                               e.ActualHeight);

                //Transform the Visual to scale
                e.LayoutTransform = new ScaleTransform(scale, scale);

                //get the size of the printer page
                System.Windows.Size sz = new System.Windows.Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                //update the layout of the visual to the printer page size.
                e.Measure(sz);
                e.Arrange(new System.Windows.Rect(new System.Windows.Point(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight), sz));

                //now print the visual to printer to fit on the one page.
                pd.PrintVisual(v, "My Print");

                //apply the original transform.
                e.LayoutTransform = originalScale;
            }
        }

        /// <summary>
        /// Save biodata to AppData
        /// </summary>
        private void ArchiveData()
        {
            Bio person = App.Current.Resources["person"] as Bio;

            if (person.Photo.Height != 1080.1507568359375) // this is very bad code
            {
                string filename = string.Format("{0}, {1}.png", arg0: person.Name_Last, arg1:person.Name_First);

                SaveBitmapImageToFile(person.Photo, appdir, filename);
            }
        }

        /// <summary>
        /// Save Bitmap image to file
        /// </summary>
        /// <param name="image">The Bitmap to be saved</param>
        /// <param name="filePath">Path to save the file</param>
        /// <param name="fileName">Name of bitmap file</param>
        /// <remarks>
        /// Adapted from StackOverflow answer by Thomas Levesque
        /// </remarks>
        /// <see>https://stackoverflow.com/a/2900564</see>
        public static void SaveBitmapImageToFile(BitmapSource image, string filePath, string fileName)
        {
            System.IO.Directory.CreateDirectory(filePath);
            using (var fileStream = new System.IO.FileStream(filePath + '\\' + fileName, System.IO.FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }

        /// <summary>
        /// Closing process for <code>MainWindow</code>
        /// </summary>
        /// <remarks>
        /// Also manually terminates all running threads.
        /// </remarks>
        /// <seealso cref="App.wackoshutdown"/>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // save user settings
            Properties.Settings.Default.Save();

            // pass close message to phototaker
            uc_PhotoTaker.Window_Closing(sender: sender, e: e);

            // kill all active threads
            Environment.Exit(0);
        }

        /// <summary>
        /// Click handler for About button in menu bar
        /// </summary>
        /// <remarks>
        /// Opens About pane
        /// </remarks>
        private void MenuItem_Help_About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        /// <summary>
        /// Click handler for Report Issues in menu bar
        /// </summary>
        /// <remarks>
        /// Opens GitHub issues page for project
        /// </remarks>
        private void MenuItem_Help_Report_Click(object sender, RoutedEventArgs e)
        {
            Uri uri;

            try {
                uri = getIssueURI();

#if DEBUG
                MessageBox.Show(
                    String.Format(
                        "Object: {0}\r\nAbsolute URI: {1}\r\nLocal Path: {2}\r\nAbsolute Path: {3}",
                        new object[] {
                            uri.GetType(),
                            uri.AbsoluteUri,
                            uri.LocalPath,
                            uri.AbsolutePath
                        }
                    ),
                    "Recieved URI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
#endif

                System.Diagnostics.Process.Start(uri.AbsoluteUri);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    ex.Message,
                    ex.GetType().ToString(),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
        }

        /// <summary>
        /// Get the URI for reporting application issues
        /// </summary>
        /// <returns>URI for reporting issues</returns>
        /// <exception cref="System.NullReferenceException">Thrown when resource for issue URI not found</exception>
        private Uri getIssueURI()
        {
            var res = Application.Current.TryFindResource("UriGitHubIssues");

            if (res == null)
            {
                throw new NullReferenceException("URI to report issues not found");
            }
            else if (!(res is string || res is Uri))
            {
                throw new NotSupportedException(
                    String.Format(
                        "Expected URI of type {0}, recieved type {1}",
                        arg0: "System.Uri",
                        arg1: res.GetType()
                        )
                    );
            }

            return res as Uri;
        }

        private void MenuItem_File_AppData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
#if DEBUG
                MessageBox.Show(
                    String.Format(
                        "Appdata Directory: {0}\r\n'appdir': {1}",
                        arg0: appdata,
                        arg1: appdir
                    ),
                    "Application Directory",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
#endif

                System.Diagnostics.Process.Start(appdir);

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    String.Format(
                        "{0}. Please try again after printing ID cards",
                        arg0: ex.Message
                        ),
                    "Photo log not located",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
        }

        private void MenuItem_Settings_Certification_Click(object sender, RoutedEventArgs e)
        {
            SettingUpdater settingUpdaterWindow = new SettingUpdater("CertificationNum", "Certification Number");
            settingUpdaterWindow.Owner = this;
            settingUpdaterWindow.ShowDialog();
        }

        private void MenuItem_Settings_PrinterName_Click(object sender, RoutedEventArgs e)
        {
            SettingUpdater settingUpdaterWindow = new SettingUpdater("PrinterName", "Printer Name");
            settingUpdaterWindow.Owner = this;
            settingUpdaterWindow.ShowDialog();
        }

        private void MenuItem_Settings_ReverseTitle_Click(object sender, RoutedEventArgs e)
        {
            SettingUpdater settingUpdaterWindow = new SettingUpdater("ReverseTitle", "Reverse Header");
            settingUpdaterWindow.Owner = this;
            settingUpdaterWindow.ShowDialog();
        }
    }



    public class Bio
    {
        public string Name_First {
            get;
            set;
        }
        public string Name_Last  {
            get;
            set;
        }
        public int Height_Feet { get; set; }
        public int Height_Inches { get; set; }
        public int Weight { get; set; }
        public DateTime Birthday { get; set; }
        public string Job_Title  { get; set; }
        public BitmapSource Photo { get; set; }
        public int HairColor { get; set; }
        public int EyeColor { get; set; }
        public int Sex { get; set; }
        public uint ID_Number { get; set; }
    }
}
