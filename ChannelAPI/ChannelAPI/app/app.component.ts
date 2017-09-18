﻿import { Component } from "@angular/core"
@Component({
    selector: "chan-mgr-app",
    template: `
                <nav class='navbar navbar-inverse' id='main-navbar'>
                    <div class='container-fluid'>
                        <ul class='nav navbar-nav'>
                            <li><a [routerLink]="['home']">Home</a><li>
                        </ul>
                    </div>
                </nav>
                <div class='container'>
                    <router-outlet></router-outlet>
                </div>
`
})

export class AppComponent {

}