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
using Pulsar4X.ViewModel;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for PlanetMineralDepositView.xaml
    /// </summary>
    public partial class PlanetMineralDepositView : UserControl
    {
        private PlanetMineralDepositVM _planetMineralDepositVM;

        public PlanetMineralDepositView()
        {
            InitializeComponent();
        }

        public void Setup(PlanetMineralDepositVM planetMineralDepositVM)
        {
            _planetMineralDepositVM = planetMineralDepositVM;
            DataContext = _planetMineralDepositVM;
        }
    }
}
