import { Component, OnInit, ViewChild, Input } from '@angular/core';
import { ChannelLogoService } from '../Service/channellogo.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ModalComponent } from 'ng2-bs3-modal/ng2-bs3-modal';
import { Observable } from 'rxjs/Rx';

@Component({
    selector:"channellogo",
    template: `
        <img [src]="image" alt="No Logo Found" *ngIf="!isLoading" />
        <div *ngIf="isLoading">Loading...</div>
`
})

export class ChannelLogoComponent implements OnInit
{
    image: any;
    isLoading: boolean = false;
    @Input() bitmapId: string = "10000";

    constructor(private _channelLogoService: ChannelLogoService) { console.log("ChannelLogoConstructor called"); }

    ngOnInit() : void {

    }
}