   M S B A M L   `   `   `  ����\  XIt.Uniba.Di.Cdg.SocialTfs.Client, Version=0.27.0.0, Culture=neutral, PublicKeyToken=nullc6clr-namespace:It.Uniba.Di.Cdg.SocialTfs.Client.Objects(It.Uniba.Di.Cdg.SocialTfs.Client.Objects  5      @    :It.Uniba.Di.Cdg.SocialTfs.Client.Containers.UIRegistration   =my6clr-namespace:It.Uniba.Di.Cdg.SocialTfs.Client.Objects  W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      ��5	      �� 5
   
     �� ImageSource$F  @/It.Uniba.Di.Cdg.SocialTfs.Client;component/Images/Wallpaper.png��6   .��5      �  � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      �� 5      $
��Auto �6   �� 5      -   5         �Name$ HostErrorRow��5      $��0 �60   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      -   5        $ UsernameErrorRow��5      $��0 �64   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      -   5        $ ErrorRow��5      $��0 �6,   �� 5      $
��Auto �6   �� 5      .+�5    
   �� $��2��6   $��0��6$   $
��5,10q�61   $��Bottom=�6?   $��28(�6Z   $
��Bold"�6h   ���	#FF293955.��6�   Registration6�   �� 5!   
   $��0��6   $��1��6    $	��5,2q�6-   .��6:   Proxy server host6M   �� 5"   
   -   5        $��	ProxyHost��5"      $��0��6C   $��1��6P   $��1��6`   $��2q�6m   6z   ;   5It.Uniba.Di.Cdg.SocialTfs.Client.Objects.UILittleBusy  5$   
   -   5          Name$ LoadingProxyHost��5$      $��Right=�64   $��Center=�6P   $��20��6k   $��20��6v   $��1��6�   $��1��6�   $��3q�6�    K� ZIndex$ 1��6�   $y�Hidden=�6�   �� 5&   
   -   5        $��HostErrorBorder��5&      $��2��6(   $��2��6<   $��2q�6I   $��1q�6T   
���   �._�5'      �� -   5        $��HostErrorLabel��5'      $��Center=�6*   5(      �� 5*   
   $��0��6   $��3��6    $	��5,2q�6-   .��6:   Email6A   �� 5+   
   -   5        $��Email��5+      $��1��6   $��3��6/   $��2,2,2,0q�6<   $��26��6M   $	��Top=�6Y   $��1��6q   6�   �� 5-   
   $��0��6   $��4��6    $	��5,2q�6-   .��6:   Invitation code6K   G� 5.   
   -	   5        $��InvitationCode��5.      $��1��6,   $��4��6<   $��2q�6I   $��2��6T   6c   �� 50   
   $��0��6   $��5��6    $	��5,2q�6-   .��6:   
Username6D   �� 51   
   -
   5        $��UserName��51      $��1��6A   $��5��6Q   $��2q�6^   $��3��6i   6x     53   
   -   5        $ LoadingUserName��53      $��Right=�63   $��Center=�6O   $��20��6j   $��20��6u   $��1��6�   $��5��6�   $��3q�6�   $ 1��6�   $y�Hidden=�6�   �� 55   
   -   5        $��UsernameErrorBorder��55      $��2��6,   $��6��6@   $��2q�6M   $��1q�6X   
���   �._�56      �� -   5        $��UsernameErrorLabel��56      $��Center=�6.   57      �� 59   
   $��0��6   $��7��6    $	��5,2q�6-   .��6:   
Password6D   G� 5:   
   -   5        $��Password��5:      $��1��6&   $��7��66   $��2q�6C   $��4��6m   6|   �� 5<   
   $��0��6   $��8��6    $	��5,2q�6-   .��6:   Confirm password6L   G� 5=   
   -   5        $��ConfirmPassword��5=      $��1��6-   $��8��6=   $��2q�6J   $��5��6t   6�   �� 5?   
   -   5        $��ErrorBorder��5?      $��2��6$   $��9��68   $��2q�6E   $��1q�6P   
���   �._�5@      �� -   5        $��
ErrorLabel��5@      $��Center=�6&   5A      =   7It.Uniba.Di.Cdg.SocialTfs.Client.Objects.UICustomButton  5C   
   =   7It.Uniba.Di.Cdg.SocialTfs.Client.Objects.UICustomButton   Click$ RegisterButton_Click��6>   $��2��6   $��10��60   $��75��6[   $��25��6f   $��5q�6r   $��Right=�6}      Text$ Register��6�      StaticBorder . 	   	IsDefault	 . 6�   �� 5E   
   $��2��6   $��11��6(   $
��5,10q�66   $��Bottom=�6D   
 �� TextAlignment$
 Center=�6_   �� 5F      -   5        .�5F   :   .,I already have an account, I want to sign in6h   5G      5H      5I      