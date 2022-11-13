﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Advocate
{
    /// <summary>
    /// Interaction logic for DescriptionHelp.xaml
    /// </summary>
    public partial class DescriptionHelpWindow : Window
    {
        public struct HelpHint
        {
            public string key { get; set; }
            public string description { get; set; }
        }

        public ObservableCollection<HelpHint> descHelpHints { get; set; }

        public void AddHelpHint(string key, string desc)
        {
            HelpHint newHint = new();
            newHint.key = key;
            newHint.description = desc;
            descHelpHints.Add(newHint);
        }

        public DescriptionHelpWindow()
        {
            InitializeComponent();
            descHelpHints = new ObservableCollection<HelpHint>();

            
            AddHelpHint("Key:", "Description:");

            // add help hints
            AddHelpHint("#AUTHOR", "The Author Name field");

            DataContext = this;
        }
    }
}
