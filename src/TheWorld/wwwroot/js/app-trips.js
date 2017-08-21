//app-trips.js
(function () {

    "use strict";

    //Creating the module
    angular.module("app-trips", ["simpleControls", "ui.router"])
        .config(function ($stateProvider, $urlRouterProvider, $locationProvider) {
            $urlRouterProvider.otherwise("main");


            $locationProvider.html5Mode(false);
            $locationProvider.hashPrefix('');
          

            $stateProvider
        .state("app", {
            url: '',
            abstract: true,
            views: {
                'side-menu@': {
                    template: '/menu/sidemenu'
                },
                'top-menu@': {
                    template: '/menu/topmenu'
                }
            }
        })
        .state('app.main', {
            url: '/main',
            views: {
                'container@': {
                    //template: "/app/main",
                    controller: function () {
                        console.log("main");
                    }
                }
            }
        })
             .state('app.login', {
                 url: '/login',
                 views: {
                     'container@': {
                         templateUrl: "/auth/login",
                         controller: function () {
                             console.log("hola");
                         }
                     }
                 }
             })
            .state('app.trips', {
                url: '/trips',
                views: {
                    'container@': {
                        templateUrl: "/views/tripEditorView.html",
                        controller: "tripsController"
                    }
                }
            });

        });
})();