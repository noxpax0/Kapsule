using System.Windows;
using System.Windows.Markup;

namespace FuturisticCtrlHud;

public static class UiStyles
{
    public static void Apply(Application app)
    {
        var dictionary = (ResourceDictionary)XamlReader.Parse("""
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="InputBorderBrush" Color="#D1D5DB" />
  <SolidColorBrush x:Key="InputTextBrush" Color="#374151" />
  <SolidColorBrush x:Key="InputMutedBrush" Color="#9CA3AF" />
  <SolidColorBrush x:Key="InputBlueBrush" Color="#2563EB" />
  <SolidColorBrush x:Key="InputErrorBrush" Color="#EF4444" />
  <SolidColorBrush x:Key="InputSuccessBrush" Color="#14B8A6" />
  <SolidColorBrush x:Key="CheckboxBorderBrush" Color="#111827" />
  <SolidColorBrush x:Key="CheckboxTickBrush" Color="#6DBB3F" />

  <Style TargetType="{x:Type TextBox}">
    <Setter Property="MinHeight" Value="44" />
    <Setter Property="FontSize" Value="15" />
    <Setter Property="Foreground" Value="{StaticResource InputTextBrush}" />
    <Setter Property="Background" Value="White" />
    <Setter Property="BorderBrush" Value="{StaticResource InputBorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Padding" Value="12,8" />
    <Setter Property="VerticalContentAlignment" Value="Center" />
    <Setter Property="CaretBrush" Value="{StaticResource InputBlueBrush}" />
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="{x:Type TextBox}">
          <Border x:Name="Border"
                  Background="{TemplateBinding Background}"
                  BorderBrush="{TemplateBinding BorderBrush}"
                  BorderThickness="{TemplateBinding BorderThickness}"
                  CornerRadius="8"
                  SnapsToDevicePixels="True">
            <ScrollViewer x:Name="PART_ContentHost"
                          Margin="{TemplateBinding Padding}"
                          VerticalAlignment="Center" />
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsKeyboardFocused" Value="True">
              <Setter TargetName="Border" Property="BorderBrush" Value="{StaticResource InputBlueBrush}" />
              <Setter TargetName="Border" Property="Effect">
                <Setter.Value>
                  <DropShadowEffect Color="#2563EB" BlurRadius="12" Opacity="0.20" ShadowDepth="0" />
                </Setter.Value>
              </Setter>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
              <Setter TargetName="Border" Property="Background" Value="#F3F4F6" />
              <Setter Property="Foreground" Value="{StaticResource InputMutedBrush}" />
              <Setter TargetName="Border" Property="BorderBrush" Value="#E5E7EB" />
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType="{x:Type CheckBox}">
    <Setter Property="MinWidth" Value="44" />
    <Setter Property="MinHeight" Value="44" />
    <Setter Property="Padding" Value="0" />
    <Setter Property="VerticalContentAlignment" Value="Center" />
    <Setter Property="Foreground" Value="{StaticResource InputTextBrush}" />
    <Setter Property="FocusVisualStyle">
      <Setter.Value>
        <Style>
          <Setter Property="Control.Template">
            <Setter.Value>
              <ControlTemplate>
                <Rectangle Margin="2"
                           Stroke="{StaticResource InputBlueBrush}"
                           StrokeDashArray="2 2"
                           StrokeThickness="2"
                           RadiusX="6"
                           RadiusY="6" />
              </ControlTemplate>
            </Setter.Value>
          </Setter>
        </Style>
      </Setter.Value>
    </Setter>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="{x:Type CheckBox}">
          <Grid MinHeight="44" SnapsToDevicePixels="True">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="50" />
              <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <Grid Width="44" Height="44" HorizontalAlignment="Left" VerticalAlignment="Center">
              <Border x:Name="Box"
                      Width="28"
                      Height="28"
                      Background="White"
                      BorderBrush="{StaticResource CheckboxBorderBrush}"
                      BorderThickness="3"
                      CornerRadius="3"
                      HorizontalAlignment="Center"
                      VerticalAlignment="Center" />
              <Path x:Name="Tick"
                    Data="M 2 25 L 16 38 L 43 4"
                    Stroke="{StaticResource CheckboxTickBrush}"
                    StrokeThickness="7"
                    StrokeStartLineCap="Round"
                    StrokeEndLineCap="Round"
                    StrokeLineJoin="Round"
                    Width="46"
                    Height="44"
                    Stretch="Fill"
                    Opacity="0" />
            </Grid>
            <ContentPresenter Grid.Column="1"
                              VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
                              RecognizesAccessKey="True"
                              Margin="2,0,0,0" />
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property="IsChecked" Value="True">
              <Setter TargetName="Tick" Property="Opacity" Value="1" />
            </Trigger>
            <Trigger Property="IsKeyboardFocused" Value="True">
              <Setter TargetName="Box" Property="BorderBrush" Value="{StaticResource InputBlueBrush}" />
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
              <Setter Property="Opacity" Value="0.55" />
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType="{x:Type Button}">
    <Setter Property="MinHeight" Value="34" />
    <Setter Property="Padding" Value="12,6" />
    <Setter Property="Foreground" Value="{StaticResource InputTextBrush}" />
    <Setter Property="Background" Value="White" />
    <Setter Property="BorderBrush" Value="{StaticResource InputBorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="{x:Type Button}">
          <Border x:Name="Border"
                  Background="{TemplateBinding Background}"
                  BorderBrush="{TemplateBinding BorderBrush}"
                  BorderThickness="{TemplateBinding BorderThickness}"
                  CornerRadius="8"
                  SnapsToDevicePixels="True">
            <ContentPresenter HorizontalAlignment="Center"
                              VerticalAlignment="Center"
                              RecognizesAccessKey="True" />
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="Border" Property="BorderBrush" Value="{StaticResource InputBlueBrush}" />
            </Trigger>
            <Trigger Property="IsKeyboardFocused" Value="True">
              <Setter TargetName="Border" Property="BorderBrush" Value="{StaticResource InputBlueBrush}" />
              <Setter TargetName="Border" Property="Effect">
                <Setter.Value>
                  <DropShadowEffect Color="#2563EB" BlurRadius="10" Opacity="0.18" ShadowDepth="0" />
                </Setter.Value>
              </Setter>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
              <Setter TargetName="Border" Property="Opacity" Value="0.82" />
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
              <Setter TargetName="Border" Property="Background" Value="#F3F4F6" />
              <Setter Property="Foreground" Value="{StaticResource InputMutedBrush}" />
              <Setter TargetName="Border" Property="BorderBrush" Value="#E5E7EB" />
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
</ResourceDictionary>
""");

        app.Resources.MergedDictionaries.Add(dictionary);
    }
}
