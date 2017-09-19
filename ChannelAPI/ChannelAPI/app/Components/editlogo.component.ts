import { Component, OnInit, ViewChild, ViewContainerRef, AfterViewInit, HostListener, Input, ElementRef } from '@angular/core';
import { ChannelService } from '../Service/channel.service';
import { ChannelLogoService } from '../Service/channellogo.service'
import { ChannelLogoComponent } from './channellogo.component';
import { IChannel } from '../Models/channel';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ModalComponent } from 'ng2-bs3-modal/ng2-bs3-modal';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/switchMap';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/of';
import 'rxjs/add/observable/from';
import 'rxjs/add/operator/toArray';

enum editLogoAction { all, single };

@Component({
    selector: 'editLogoForm',
    templateUrl: 'app/Components/editlogo.component.html',
    styleUrls: ['app/Styles/editlogo.component.css']
})

export class EditLogoForm implements OnInit
{
    @ViewChild('modalChLogo') modalChLogo: ModalComponent;
    @Input() set channel(value: IChannel) {
        console.log("set channel called");
        if (value && value != this.channel && this.action == editLogoAction.all) {
            this._channel = value;
            this.loadStations();
            this.showForm = true;
            console.log('opening modal');
            this.modalChLogo.open();
        }
    };
    get channel(): IChannel {
        return this._channel;
    }
    private _channel: IChannel;
    isLoading: boolean;
    public showForm: boolean = false;
    showStations: boolean = true;
    logoForm: FormGroup;
    action: editLogoAction;
    stations: any[];
    errorMsg: string;
    newImage: any;

    constructor(private fb: FormBuilder, private _channelService: ChannelService, private _channelLogoService: ChannelLogoService, private element: ElementRef)
    {
        this.action = editLogoAction.all;
    }

    ngOnInit() {
        this.isLoading = true;
        if (null == this.channel) {
            this.showForm = false;
            return;
        };
        this.isLoading = false;
    }

    onActionChange($obj) {
        console.log("onActionChange called");
        var descElement = document.getElementById('action-desc');
        if ($obj.target.value == 'all')
        {
            this.action = editLogoAction.all;
            descElement.innerHTML = "Updates the logo for all stations to which this logo is currently assigned (listed below)."
            this.showStations = true;
        }
        else
        {
            this.action = editLogoAction.single;
            descElement.innerHTML = "Creates a new logo and assigns it only to this station."
            this.showStations = false;
        }
    }

    loadStations()
    {
        this._channelLogoService.getBy('api/channellogo/{0}/station', this.channel.intBitMapId).subscribe(x => {
            this.stations = x;
        }, error => this.errorMsg = <any>error);
    }

    getUri(bitmapId: number) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }

    newImageChange($event) {
        console.log("newImageChange called.")
        var reader = new FileReader();

        var image = document.getElementById('newImage');

        reader.onload = function (e) {
            var src = reader.result;
            image.setAttribute('src', src);
        }

        reader.readAsDataURL($event.target.files[0]);
        this.newImage = image.getAttribute('src');
    }
}