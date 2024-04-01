# ag.WPF.UpDown

Custom WPF UpDown control with button spinners that allows incrementing and decrementing numeric values by using the spinner buttons and keyboard up/down arrows

![Nuget](https://img.shields.io/nuget/v/ag.WPF.UpDown) 

![ag.WPF.UpDown](https://am3pap005files.storage.live.com/y4mSptr4gIhux74b5jb3_nGOZ52MC6ui2oyA1iNWoahGuYdG6ziKy0w1500c-DpaMWzVoFBfgyngSpM9l4fo2Y2jdoDGc7g7QKCFUzNvTPLnv-6hOlB7EZ8o9S3WKjNqAz-Glcs4tSfUb9aVI4iSS-popd7YZTOVTR6t-mHZ_bQvxaZvf1_C1P_HEjjQgNZwwuY?width=122&height=60&cropmode=none "ag.WPF.UpDown")

## Installation

Use Nuget packages

[ag.WPF.UpDown](https://www.nuget.org/packages/ag.WPF.UpDown/)

## Usage

```csharp
<Window x:Class="TestUpDown.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:upd="clr-namespace:ag.WPF.UpDown;assembly=ag.WPF.UpDown"
        xmlns:local="clr-namespace:TestUpDown"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800">
    <Grid>
        <upd:UpDown Width="100" IsReadOnly="False" MaxValue="100" MinValue="-100" NegativeForeground="Red" Step="1" DecimalPlaces="0"/>
    </Grid>
</Window>
```

## Properties

Property name | Return value | Description | Default value
--- | --- | --- | ---
DecimalPlaces | uint | Gets or sets the value that indicates the count of decimal digits shown at UpDown | 0
IsReadOnly | bool | Gets or sets the value that indicates whether UpDown is in read-only state | True
MaxValue | decimal | Gets or sets the maximum allowed value of UpDown | 100
MinValue | decimal | Gets or sets the minimum allowed value of UpDown | 0
NegativeForeground | SolidColorBrush | Gets or sets the Brush to apply to the text contents of UpDown when control's value is negative | Red
Step | decimal | Gets or sets the value to increment or decrement UpDown when the up or down buttons are clicked | 1
UseGroupSeparator | bool | Gets or sets the value that indicates whether group separator is used for number formatting | True
Value | decimal | Gets or sets the value of UpDown | 0
ShowTrailingZeros | bool | Gets or sets the value that indicates whether trailing zeros in decimal part of UpDown should be shown | True

## Events

Event | Description
--- | ---
DecimalPlacesChanged |  Occurs when the DecimalPlaces property has been changed in some way
IsReadOnlyChanged | Occurs when the IsReadOnly property has been changed in some way
MaxValueChanged | Occurs when the MaxValueChanged property has been changed in some way
MinValueChanged | Occurs when the MinValue property has been changed in some way
StepChanged | Occurs when the Step property has been changed in some way
UseGroupSeparatorChanged | Occurs when the UseGroupSeparator property has been changed in some way
ValueChanged | Occurs when the Value property has been changed in some way