using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FGDeliveryWPF.SalesServices;

namespace FGDeliveryWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer getTimer = null;
        public MainWindow()
        {
            InitializeComponent();

            CreateHeader().GetAwaiter();

            //Create timer so that to get the Tickets information periodically from Dynamics.
            getTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 15)
            };
            getTimer.Tick += GetTimer_Tick;
            getTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
             FillTable().GetAwaiter();            
        }

        private void GetTimer_Tick(object sender, EventArgs e)
        {            
            FillTable().GetAwaiter();            
        }

        /// <summary>
        /// Create header lines on a Grid. Gate number and Line numbers
        /// </summary>
        private async Task CreateHeader()
        {
            //Get styles from Application resources
            Style colHeader = Application.Current.FindResource("ColHeader") as Style;
            Style sGate = Application.Current.FindResource("HeaderGate") as Style;

            SalesServiceClient serviceClient = new SalesServiceClient();
            //Get Lines and Gates information from Dynamics as a Async method
            var lines = await serviceClient.GetFGLinesAsync();

            if (lines.Count() > 0)
            {
                int start = 0;
                float size = 100 / lines.Count();// get the estimated width of a single column
                //Receive all Gates information
                var gates = lines.Select(t => t.onGateNumField).Distinct().ToList();
                for(int i=0; i<gates.Count; i++)
                {
                    var count = lines.Select(t => t.onGateNumField).Count(t => t.Equals(gates[i]));

                    Label tbGate = new Label()
                    {
                        Content = "Gate " + gates[i].ToString(),
                        Style = sGate                      
                        
                    };
                    gridMain.Children.Add(tbGate);
                    Grid.SetColumn(tbGate, start);
                    Grid.SetColumnSpan(tbGate, count);//Span the cell 
                    start = start + count;
                }

                //Get the FG Lines information and produce it to Column Headers
                foreach (FGLineContract line in lines)
                {
                    ColumnDefinition dt = new ColumnDefinition();
                    dt.Width = new GridLength(size, GridUnitType.Star);                    
                    gridMain.ColumnDefinitions.Add(dt);

                    Border bOnStack = new Border()
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0, 0, 1, 0),
                        Tag = line.fGLineNumberField.ToString()
                    };

                    StackPanel sp = new StackPanel();                    
                    sp.Tag = line.fGLineNumberField.ToString();
                    sp.HorizontalAlignment = HorizontalAlignment.Stretch;
                    bOnStack.Child = sp;
                   
                    sp.Children.Add(new TextBlock()
                    {
                        Text = line.fGLineNameField,    
                        Style = colHeader
                    });

                    gridMain.Children.Add(bOnStack);
                    Grid.SetColumn(bOnStack, line.fGLineNumberField-1);
                    Grid.SetRow(bOnStack, 1);
                   
                }
            }
            serviceClient.Close();
            gridMain.ApplyTemplate();
        }

        /// <summary>
        /// Get all the deliveries and show them on Grid
        /// </summary>
        /// <returns></returns>
        private async Task FillTable()
        {
            List<FGDeliveryContract> lines = new List<FGDeliveryContract>();
            SalesServiceClient serviceClient = new SalesServiceClient();
            //receive the deliveries from Dynamics as a Async method
            var deliveries = await serviceClient.GetDeliveriesAsync(DateTime.Now);
            if (deliveries.Count() > 0)
            {
                //Get styles from Application resources.
                Style cellFirst = Application.Current.FindResource("CellFirst") as Style;
                Style cellBlock = Application.Current.FindResource("CellBlock") as Style;

                lines = deliveries.ToList();
                foreach (var coll in gridMain.Children)//find all columns in a main grid
                {
                    //the column's outer most control is Border
                    if (coll is Border)
                    {
                        Border br = coll as Border;
                        //Get the line number from its Tag property
                        int lineNum = int.Parse(br.Tag.ToString());
                        
                        //inside the border is a Stack Panel.
                        StackPanel sp = br.Child as StackPanel;
                        
                        //remove all the controls except first TextBlock
                        if (sp.Children.Count > 1)
                            sp.Children.RemoveRange(1, sp.Children.Count - 1);

                        //filter data by current line number
                        var subDeliv = lines.FindAll(t => t.lineNumField == lineNum);
                        foreach (FGDeliveryContract contract in subDeliv)
                        {
                            //add ticket number line 
                            TextBlock txtCell=new TextBlock()
                            {
                                Text = contract.ticketField + " - " + contract.truckPlateNumField,
                                Style = cellBlock
                            };
                            //apply "waiting truck" style if it is not top most truck
                            if(txtCell.Text.StartsWith(subDeliv.First().ticketField))
                                txtCell.Style = cellFirst;

                            sp.Children.Add(txtCell);
                        }
                    }
                }

            }
            serviceClient.Close();
            
        }

        

        
    }
}
