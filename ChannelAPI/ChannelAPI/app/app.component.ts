import { Component } from "@angular/core"
@Component({
    selector: "app",
    template: `
                <nav class='navbar navbar-inverse bg-black3d' id='main-navbar'>
                    <div class='container-fluid'>
                        <ul class='nav navbar-nav'>
                            <li><a [routerLink]="['home']">Home</a><li>
                        </ul>
                    </div>
                </nav>
                <div class='container body-content' id='content-container'>
                    <router-outlet></router-outlet>
                </div>
`
})

export class AppComponent {

}