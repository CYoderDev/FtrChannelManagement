import { NgModule } from '@angular/core';
import { APP_BASE_HREF } from '@angular/common';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BsModalModule } from 'ng2-bs3-modal';
import { HttpModule } from '@angular/http';
import { AgGridModule } from 'ag-grid-angular/main'
import { AppComponent } from './app.component';
import { routing } from './app.routing';
import { HomeComponent } from './Components/home.component';
import { ChannelService } from './Service/channel.service';
import { ChannelComponent } from './Components/channel.component';
//import { ChannelLogoComponent } from './Components/channellogo.component';
import { EditLogoForm } from './Components/editlogo.component';
import { ChannelInfoComponent, FocusableInput } from './Components/channelinfo.component';
import { ChannelLogoService } from './Service/channellogo.service';
import { ImageCellRendererComponent } from './Components/imagecellrender.component';
import { HeaderComponent } from './Components/header.component';
import { ConsoleLogService } from './Logging/consolelogger.service';
import { Logger } from './Logging/default-logger.service';

@NgModule({
    imports: [BrowserModule, HttpModule, routing, FormsModule, BsModalModule, AgGridModule.withComponents(
        [
            HeaderComponent,
            ImageCellRendererComponent
        ]
    )],
    declarations: [AppComponent, ChannelComponent, HeaderComponent, EditLogoForm, ChannelInfoComponent, ImageCellRendererComponent, FocusableInput],
    providers: [{ provide: APP_BASE_HREF, useValue: '/' }, { provide: Logger, useClass: ConsoleLogService }, ChannelService, ChannelLogoService],
    bootstrap: [AppComponent]
})
    
export class AppModule { }