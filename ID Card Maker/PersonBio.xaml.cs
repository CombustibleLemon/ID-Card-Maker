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
    /// Interaction logic for PersonBio.xaml
    /// </summary>
    public partial class PersonBio : UserControl
    {
        private readonly int AdditionalAttributeIndexBegin = 3;
        private readonly int AdditionalAttributeIndexEnd = 5;

        public PersonBio()
        {
            InitializeComponent();
        }

        private void Input_Text_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            BindingExpression textProperty = textBox.GetBindingExpression(TextBox.TextProperty);
            GC.Collect();
            textProperty.UpdateSource();
        }

        public void ShowAdditionalAttributes()
        {
            
            for (int i = AdditionalAttributeIndexBegin; i <= AdditionalAttributeIndexEnd; i++)
            {
                MainGrid.RowDefinitions[i].Height = GridLength.Auto;
            }
        }

        public void HideAdditionalAttributes()
        {

            for (int i = AdditionalAttributeIndexBegin; i <= AdditionalAttributeIndexEnd; i++)
            {
                MainGrid.RowDefinitions[i].Height = new GridLength(0, GridUnitType.Pixel);
            }
        }
    }
}
