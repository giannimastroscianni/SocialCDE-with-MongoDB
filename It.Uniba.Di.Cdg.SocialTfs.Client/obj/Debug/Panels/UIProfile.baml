   M S B A M L   `   `   `  ����\  XIt.Uniba.Di.Cdg.SocialTfs.Client, Version=0.27.0.0, Culture=neutral, PublicKeyToken=nullc6clr-namespace:It.Uniba.Di.Cdg.SocialTfs.Client.Objects(It.Uniba.Di.Cdg.SocialTfs.Client.Objects  5      a5clr-namespace:It.Uniba.Di.Cdg.SocialTfs.Client.Panels'It.Uniba.Di.Cdg.SocialTfs.Client.Panels  5      7    1It.Uniba.Di.Cdg.SocialTfs.Client.Panels.UIProfile   5      =my6clr-namespace:It.Uniba.Di.Cdg.SocialTfs.Client.Objects  ?local5clr-namespace:It.Uniba.Di.Cdg.SocialTfs.Client.Panels  5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      ���	#FF9CAAC1.��5	      � -   5        $��	Container��5	      .+�5
   
   � -   5        $��ProfileGrid��5
        � RowDefinitions  5      �� 5      $��74 �6    �� 5      .+�5      �� ���	#FF9CAAC1._�5      � $	��285��6   $��5q�6#   $
��Left=�6.    � ColumnDefinitions 5      �� 5      $��74 �6+   �� 5      �� 5      �� 5        5      �� 5      $
��Auto �6(   �� 5      �� 5      $
��Auto �6(   �� 5      $
��Auto �6(   .+�5      �� $��2��6!   $��2��61   $��Right=�6E    �� Orientation$ 
Horizontal=�6a   .+�5      =   7It.Uniba.Di.Cdg.SocialTfs.Client.Objects.UICustomButton  -   5          Name$ 
MoreButton��5   ,   =   7It.Uniba.Di.Cdg.SocialTfs.Client.Objects.UICustomButton   Click$ 
More_Click��6W   $��22��6@   $��22��6L    � ToolTip$ View your skills��6j     5      -   5        $ SettingsButton��5   ,   $ Setting_Click��6[   $��22��6D   $��22��6P   $ Change your password��6q     5       -   5        $ 
ExitButton��5    ,   $ 
Exit_Click��6W   $��22��6@   $��22��6L   $ Logout��6j   5!        5"      -   5        $ Avatar��5"   (   $ Avatar_Click��68   $ Change your avatar��6M   $��Center=�6j   $��Center=�6�   $��4��6�   �� 5#      -   5        $��UserName��5#       $��2q�60   $	��Top=�6;   $
��Left=�6S   $��1��6n   $��2��6~   $��2��6�   $��16(�6�   �� 5$      -   5        $��Posts��5$       $��1q�6-   $��Center=�68   $��Center=�6S   $��1��6p   $��2��6�   $��14(�6�   �� 5%      $��1��6    $��Right=�6*   $��Stretch=�6F   $��1��6b   $��2��6r   $��2��6   
���   �#��["�6�   �� 5&      -	   5        $��
Followings��5&       $��1q�62   $��Center=�6=   $��Center=�6X   $��2��6u   $��2��6�   $��14(�6�   �� 5'      $��1��6    $��Right=�6*   $��Stretch=�6F   $��2��6b   $��2��6r   $��2��6   
���   �#��["�6�   �� 5(      -
   5        $��	Followers��5(       $��1q�61   $��Center=�6<   $��Center=�6W   $��3��6t   $��2��6�   $��14(�6�   �� 5)      $��Center=�6    $��Center=�6;   $��1��6X   $��3��6h   posts6}   �� 5*      $��Center=�6    $��Center=�6;   $��2��6X   $��3��6h   	following6�   �� 5+      $��Center=�6    $��Center=�6;   $��3��6X   $��3��6h   	followers6�   5,      5-      �� 5.      -   5        $��ServicesScroll��5.      ��. $��2��6J   
�������.��5/      � $
��Auto=�6   .+�50      �� 
�������.+�51      �� -   5        $��ServicesLabel��51   $   $��16(�69   $
��20,5q�6G   	Services:6`   � 52      -   5        $��Services��52   $   $	��285��64   $
��Auto=�6C   $��15,5,0,5q�6s   .+�53      �� $��5q�6(   $
��Wrap=�63   &$There are no services available yet.�� 6l   .,Please try again soon or contact your admin.54   O   55      56      57      58      5:      5;      5<      