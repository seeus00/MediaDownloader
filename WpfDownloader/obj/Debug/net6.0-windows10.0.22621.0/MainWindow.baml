   M S B A M L   `   `   `  ����T  PMahApps.Metro, Version=2.0.0.0, Culture=neutral, PublicKeyToken=51482d6f650b2b3fV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls  51   	    WpfDownloader   WpfDownloader.MainWindow   5-   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5.   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5/   	   @d2http://schemas.microsoft.com/expression/blend/2008     50   	   Kmah;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     51   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     52   	   -   5          � Title$  Casey's Downloader��53   	   $	��450��6$   $	��800��61   ���#202020
�������$��15(�56   	    � Icon$ /res/icon.png��57   	   (   "MahApps.Metro.Controls.MetroWindow   ShowIconOnTitleBar$
 True��58   	      WindowTitleBrush �#202020   TitleBarHeight$ 26��5=   	      WindowTransitionsEnabled$ False��5>   	      TitleCharacterCasing$ Normal=�5?   	       NonActiveWindowTitleBrush �#202020$��1q�5A   	      NonActiveGlowBrush �#202020	   NonActiveBorderBrush	 �#202020
 � 	Resources
 5E       �� 
TargetType �� Property �� Value �� 
TargetType �� Orientation K� IsItemsHost��%c  'o�       �� 5F   
   " o�6&   .�5G      ��  �@�OverridesDefaultStyle$
 True��66   �� 5H       �@J�SnapsToDevicePixels$
 True��64   �� 5I       � �� 5J      �� 5K      " o�6&   .R�5M      �� $��0q�6!   ._�5N      �� $
}�True��6)   $ 
Horizontal=�6=   $
 True��5O      5P      5Q      5S      5T      5U      .��5W      � .+�5X   
   ��  +� FocusedElement 6   ��  �� ElementName$ 
UrlTextBox��.+�5Y      ]� .+�5Z      o� $	��Top=�6   .@�5[      m� $��_File��6   .@�5\      m� -   5        $��ContextMenuItem��5]   #   $��
_Open list��5\   #   5]   #   5^      5_      5`      �� 5a      -   5         ��Name$ 
UrlTextBox��5a   *   $��
60 10 60 0q�6   $��15(�6>   $
|�True��6L   6_   � 5c      $��10q�6    � ColumnDefinitions 5d      �� 5e      $��* �6'   �� 5f      $��5* �6'    � RowDefinitions 5h      �� 5i      $
��auto �6$   �� 5j      $
��auto �6$   .+�5l      � $��0��6   $��5q�6'    5m      �� 5n      $��* �6+   �� 5o      $��4* �6+    5q      �� 5r      $
��auto �6(   �� 5s      $
��auto �6(   �� 5t      $
��auto �6(   .+�5v      �� -   5        $��DownloadingIcon��5v   :   $��/res/Downloading.png��6   $��0 0 5 10q�6Q   $��0��6c   $��0��6s   $	��100��6�   $��20��6�   �� 5w      -   5        $��FinishedIcon��5w   7   $��/res/Finished.png��6   $��0 0 5 10q�6K   $��0��6]   $��1��6m   $	��100��6z   $��20��6�   �� 5x      -   5        $��	ErrorIcon��5x   4   $��/res/Error.png��6   $��0 0 5 10q�6E   $��0��6W   $��2��6g   $	��100��6t   $��20��6�   �� 5y      -   5        $��DownloadingTextBlock��5y   �   $��12(�6    $��1��6.   $��0��6>   $��Downloading��6�   ��6K   �� $ entryTextBox�� �� Path$ ActualWidth�6�   �� 5z      -   5        $��FinishedTextBlock��5z   �   $��12(�6    $��1��6.   $��1��6>   $��Finished��6�   ��6K   �� $ entryTextBox��$ ActualWidth�6�   �� 5{      -	   5        $��ErrorTextBlock��5{   �   $��12(�6    $��1��6.   $��2��6>   $��Errors��6�   ��6K   �� $ entryTextBox��$ ActualWidth�6�   5|      �� 5}      -
   5        $��UrlsListView��5}   L   $��1��5~      $��0��5      $��10 0 0 0q�5�      $��-1q�5�      $	��Top=�5�      $��2q�5�      
���������5�      �� 5�      " ��6    .�5�      -   5        �� 5�       � ��
 �����6O   5�       �� View 5�      �� 5�      .P�5�      ��  �� Width$ 60��6-   ��5�   "   �� 5�   &   -   5        
 � Tag$ Number��5�   ;   .��6o   #6r    �� CellTemplate 5�   "   �� 5�   &   .R�5�   *   �� $��0 3 0 3q�61   ._�5�   .   �� $��NumberTextBlock��68   ��6O   �� $ Number�5�   +   5�   '   5�      �� 5�      $	 400��6-   ��5�   "   �� 5�   &   -   5        $
 Name��5�   ;   .��6m   Name6s    5�   "   �� 5�   &   .R�5�   *   �� $ 
Horizontal=�65   .+�5�   .   �� $��SiteIconImage��6R   $��0 0 15 0q�64   $��20��6F   ��6g   �� $ ImgIconPath�6�   �� 5�   .   $��NameTextBlock��68   ��6M   �� $
 Name�5�   +   5�   '   5�      �� 5�      $	 130��6-   ��5�   "   �� 5�   &   -   5        $ 	StatusMsg��5�   ;   .��6t   Status6|    5�   "   �� 5�   &   .R�5�   *   �� $��StatusTextBlock��6O   $
��Left=�64   ��6f   �� $ 	StatusMsg�5�   '   5�      �� 5�      $	 120��6-   ��5�   "   �� 5�   &   ${�False��6;   
�������$ ProgressHeader��6`   .��6v   
Progress6�    5�   "   �� 5�   &   .R�5�   *   �� $��4q�61   ._�5�   .   � $	��110��63   .+�5�   2   -   'MahApps.Metro.Controls.MetroProgressBar  $��DownloadBar��6�   $��Center=�6G   $	��110��6�    � Minimum$ 0Q�6�    � Maximum$	 100Q�6�   
��� � � � Value 6d   �� $ 	Bar.Value�6�   �� 5�   2   $��0 0 10 0q�5�   5   $��Center=�5�   5   $��Center=�5�   5   
���   ���5�   <   �� $ DownloadBar��$ Value� �� StringFormat$ {0:0.0}%��5�   5   5�   /   5�   +   5�   '   5�      �� 5�      $	 100��6-   ��5�   "   �� 5�   &   ${�False��6;   
�������$ FilesHeader��6`   .��6s   Files6z    5�   "   �� 5�   &   .R�5�   *   �� $��0 3 0 3q�61   ._�5�   .   �� $��FilesTextBlock��68   ��6N   �� $ FilesMsg�5�   +   5�   '   5�      �� 5�      $ 80��6-   ��5�   "   �� 5�   &   ${�False��6;   
�������$ CancelButton��6`   6v    5�   "   �� 5�   &   .R�5�   *   �� $��CancelButton��6J   -   5        $��12(�5�   1   $��65��6?    '  #MahApps.Styles.Button.Square.Accent#��[  5�   1   y�5�   1   �� $ CancelButton.Visibility�.��6e   Cancel6m   5�   '   5�      5�      ��5�      �� 5�      " ��6     �� Triggers 5�      P� 5�   "     P� Property  �@J�IsMouseOver! P� Value$
! True��6A   . �5�   &   ��  � ��
 �iii�6C   5�   #   .�5�      ��  � �� 5�   "   �� 5�   &   " ��66   .R�5�   *   �� 
������ $��0q�5�   -   #��z��5�   -   ._�5�   .   �� $��Stretch=�6C   #��z��6a   $
��Auto��6�   $��0q�6�   " �� Content#" z��6�   5   +   5  '   5     �� 5      � �� 6;   �� %  System.Windows.SystemParameters#   WindowGlassBrush$ �� Source#$ Z# 6�   5     5     5	     5
     5     5     