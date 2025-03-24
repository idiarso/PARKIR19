/**
 * Menu Navigation Fix
 * Enhances the navigation menu functionality in PARKIR_WEB application
 */
(function($) {
    "use strict";

    $(document).ready(function() {
        // Mobile menu toggle
        $(".geex-header__menu-toggle").on("click", function(e) {
            e.preventDefault();
            $(".geex-header__menu-wrapper").toggleClass("active");
            $("body").toggleClass("menu-open");
        });

        // Add active class to current menu item
        const currentPath = window.location.pathname;
        
        // Handle submenu items for desktop
        $(".geex-header__menu__item.has-children").on("mouseenter", function() {
            $(this).addClass("hover");
        }).on("mouseleave", function() {
            $(this).removeClass("hover");
        });

        // For touch devices - toggle submenu on click
        $(".geex-header__menu__item.has-children > .geex-header__menu__link").on("click", function(e) {
            if (window.innerWidth > 991) {
                if (!$(this).parent().hasClass("hover")) {
                    e.preventDefault();
                    $(this).parent().addClass("hover");
                }
            } else {
                e.preventDefault();
                $(this).parent().toggleClass("active");
                $(this).siblings(".geex-header__submenu").slideToggle();
            }
        });

        // Find active menu item based on current path
        $('.geex-header__menu__link, .geex-header__submenu .geex-header__menu__link').each(function() {
            const menuLink = $(this).attr('href');
            
            if (menuLink && currentPath.indexOf(menuLink) > -1) {
                $(this).addClass('active');
                
                // If it's a submenu item, also highlight parent
                if ($(this).closest('.geex-header__submenu').length) {
                    $(this).closest('.geex-header__menu__item.has-children')
                           .addClass('active')
                           .find('> .geex-header__menu__link')
                           .addClass('active');
                }
            }
        });

        // Close mobile menu when clicking outside
        $(document).on("click", function(e) {
            if (
                $(e.target).closest(".geex-header__menu-wrapper").length === 0 &&
                $(e.target).closest(".geex-header__menu-toggle").length === 0 &&
                $(".geex-header__menu-wrapper").hasClass("active")
            ) {
                $(".geex-header__menu-wrapper").removeClass("active");
                $("body").removeClass("menu-open");
            }
        });

        // Add missing menu toggler for mobile if not present
        if ($(".geex-header__menu-toggle").length === 0) {
            $(".geex-header").prepend(
                '<button class="geex-header__menu-toggle">' +
                '<span></span><span></span><span></span>' +
                '</button>'
            );

            // Re-bind the event
            $(".geex-header__menu-toggle").on("click", function(e) {
                e.preventDefault();
                $(".geex-header__menu-wrapper").toggleClass("active");
                $("body").toggleClass("menu-open");
            });
        }
    });

})(jQuery); 