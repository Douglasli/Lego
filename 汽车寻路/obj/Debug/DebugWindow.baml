   M S B A M L   `   `   `  ����  汽车轨迹!    WpfApplication1.DebugWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  DebugWindow��5   	   $	��300��6   $	��300��6*   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      �� 5	      $
��auto �6    � ColumnDefinitions 5   
   �� 5      �� 5      $
��Auto �6   �� 5      .+�5   
   �� -   5        $
��grid��5      $��0��6   $��2��6*   $��0��6;   �� 5   
   $��0��6   $��1��6   $��20��6/   $��Stretch=�6;   ��5      ��  �� ElementName$ layerScrollBar�� �� Path$ Value� �� Mode$ TwoWay=��� 5   
   -   5        $��layerScrollBar��5      $��1��6'   $��1��64    �� Orientation$ Vertical=�6D    �� TickPlacement$ BottomRight=�5       �� IsDirectionReversed . 	 �� TickFrequency$	 1Q�6H   
 �� IsSnapToTickEnabled
 . 5      �� 5   
   -   5        $��pauseButton��5      $��2��6$   $��1��61   $��暂停��6A   6N   5"      5#      