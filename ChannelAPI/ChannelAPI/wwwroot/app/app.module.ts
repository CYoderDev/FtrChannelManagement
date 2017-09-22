import { NgModule } from '@angular/core';
import { APP_BASE_HREF } from '@angular/common';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Ng2Bs3ModalModule } from 'ng2-bs3-modal/ng2-bs3-modal';
import { HttpModule } from '@angular/http';
import { AgGridModule } from 'ag-grid-angular/main'
import { AppComponent } from './app.component';
import { routing } from './app.routing';
import { HomeComponent } from './Components/home.component';
import { ChannelService } from './Service/channel.service';
import { ChannelComponent } from './Components/channel.component';
import { ChannelLogoComponent } from './Components/channellogo.component';
import { EditLogoForm } from './Components/editlogo.component';
import { ChannelInfoComponent } from './Components/channelinfo.component';
import { ChannelLogoService } from './Service/channellogo.service';
import { HeaderComponent } from './Components/header.component';
import { OrderBy } from './order-by.pipe';

@NgModule({
    imports: [BrowserModule, ReactiveFormsModule, HttpModule, routing, Ng2Bs3ModalModule, FormsModule, AgGridModule.withComponents(
        [
            HeaderComponent
        ]
    )],
    declarations: [AppComponent, ChannelComponent, ChannelLogoComponent, OrderBy, HeaderComponent, EditLogoForm, ChannelInfoComponent],
    providers: [{ provide: APP_BASE_HREF, useValue: '/' }, ChannelService, ChannelLogoService],
    bootstrap: [AppComponent]
})

export class AppModule { }