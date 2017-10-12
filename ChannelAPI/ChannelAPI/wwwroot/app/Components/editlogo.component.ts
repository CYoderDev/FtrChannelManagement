import { Component, OnInit, ViewChild, ElementRef, Input, Output, EventEmitter } from '@angular/core';
import { ChannelLogoService } from '../Service/channellogo.service'
import { IChannel } from '../Models/channel';
import { BsModalComponent } from 'ng2-bs3-modal';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/switchMap';
import 'rxjs/add/operator/do';
import 'rxjs/add/observable/of';

enum editLogoAction { all, single };

@Component({
    selector: 'editLogoForm',
    templateUrl: 'app/Components/editlogo.component.html',
    styleUrls: ['app/Styles/editlogo.component.css']
})

export class EditLogoForm implements OnInit
{
    @ViewChild('modalChLogo') private modalChLogo: BsModalComponent;
    @ViewChild('inputImg') private inputImg: ElementRef;
    @Input() set channel(value: IChannel) {
        if ((!this.channel && value) || value && (value.strFIOSServiceId != this.channel.strFIOSServiceId
            || this.channel.dtCreateDate != value.dtCreateDate)) {
            console.log("set channel called");
            this._channel = value;
            this.imgSource = this.getUri(value.intBitMapId);
            if (!this.stations && this.action == editLogoAction.all && this.showForm)
                this.loadStations();
        }
    };

    @Output() channelchange: EventEmitter<string> = new EventEmitter<string>();

    get channel(): IChannel {
        return this._channel;
    }
    private _channel: IChannel;
    private isLoading: boolean;
    public showForm: boolean = false;
    private showStations: boolean = true;
    private stationsLoading: boolean = false;
    private action: editLogoAction;
    public stations: any[];
    errorMsg: string;
    isSuccess: boolean = false;
    newImage: any;
    public imgSource: any;
    private duplicateIds: string[];
    private submitting: boolean = false;

    constructor(private _channelLogoService: ChannelLogoService)
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

    private onActionChange($obj) {
        console.log("onActionChange called");
        this.isSuccess = false;
        var descElement = document.getElementById('action-desc');
        if ($obj.target.value == 'all')
        {
            this.loadStations();
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

    OpenForm() {
        console.log('opening edit logo modal');
        if (this._channel)
            this.loadStations();
        this.modalChLogo.open();
    }

    private loadStations()
    {
        console.log('Loading stations from edit logo modal');
        this.stationsLoading = true;
        this._channelLogoService.getBy('api/channellogo/{0}/station', this.channel.intBitMapId)
            .subscribe(x => {
                this.stations = x;
            }, error => this.errorMsg = <any>error, () => { this.stationsLoading = false; });
    }

    getUri(bitmapId: number) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }

    private newImageChange($event) {
        console.log("newImageChange called.")
        if (!$event || !$event.target.files || $event.target.files.length < 1)
            return;

        this.isSuccess = false;
        var reader = new FileReader();

        var image = document.getElementById('newImage');

        reader.onload = function (e) {
            var src = reader.result;
            image.setAttribute('src', src);
        }

        reader.readAsDataURL($event.target.files[0]);
        this.newImage = $event.target.files[0];
    }

    private onSubmit() {
        var nextId: number;
        var duplicate: boolean;

        this.getDuplicates()
            .switchMap((value) => {
                if (!value) {
                    duplicate = false;
                    return this.getNextId();
                }
                else {
                    duplicate = true;
                    return Observable.of(value);
                }
            })
            .subscribe((observer) => {
                nextId = parseInt(observer);
                if (isNaN(nextId))
                    throw new Error("Failed to get image identifier.");
            }, (error) => {
                console.log(error);
                this.errorMsg = error;
            }, () => {
                if (this.errorMsg)
                    return;

                 //Assign all stations to duplicate id
                if (this.action == editLogoAction.all && duplicate) {
                    this.updateStation(this.stations, nextId);
                }
                //Update logo image for existing ID
                else if (this.action == editLogoAction.all && !duplicate) {
                    this.updateLogo(this.channel.intBitMapId);
                }
                //Assign this station to duplicate id
                else if (duplicate) {
                    this.updateStation([this.channel], nextId);
                }
                //Create new id and new image file and assign to this station
                else {
                    this.createLogo(this.channel.strFIOSServiceId, nextId);
                }
            });
    }

    private getDuplicates() : Observable<any> {
        console.log('getDuplicates called');
        this.submitting = true;
        return this._channelLogoService.performRequest('/api/channellogo/image/duplicate', 'PUT', this.newImage, 'application/octet-stream', this.newImage.type);
    }

    private getNextId(): Observable<any> {
        console.log('getNextId called');
        return this._channelLogoService.get('/api/channellogo/nextid');
    }

    private updateStation(stations: any[], bitmapId: number, index = 0) {

        if (index >= stations.length)
            return;

        this._channelLogoService.performRequest('/api/channellogo/' + bitmapId + '/station/' + stations[index].strFIOSServiceId, 'PUT', null, 'application/json')
            .subscribe((observer) => {
                console.log('%i stations updated with id %s', observer, stations[index].strFIOSServiceId);
            }, (error) => {
                console.log('Failed to update station id %s. %s', stations[index].strFIOSServiceId, error);
                this.errorMsg = 'Failed to update station - ' + error;
            }, () => {
                this.inputImg.nativeElement.value = "";
                if (index + 1 == stations.length) {
                    this.loadStations();
                    if (!this.errorMsg) {
                        this.isSuccess = true;
                    }
                    this.submitting = false;
                }
                this.channelchange.emit(stations[index].strFIOSServiceId);
                this.updateStation(stations, bitmapId, ++index);
            });
    }

    private updateLogo(bitmapId: number) {
        console.log('updateLogo called', bitmapId);
        return this._channelLogoService.performRequest('api/channellogo/image/' + bitmapId.toString(), 'PUT',
            this.newImage, 'application/octet-stream', this.newImage.type)
            .subscribe((val) => {
                this.channel.intBitMapId = bitmapId;
            }, (error) => {
                console.log(error);
                this.errorMsg = "Failed to update logo. " + error;
            }, () => {
                this.inputImg.nativeElement.value = "";
                this.submitting = false;
                if (!this.errorMsg) {
                    this.isSuccess = true;
                    this.stations.forEach((station) => {
                        this.channelchange.emit(station.strFIOSServiceId);
                    })
                }
            });
    }

    private createLogo(fiosid: string, bitmapId: number) {
        console.log('createLogo called', fiosid, bitmapId);
        return this._channelLogoService.performRequest('/api/station/' + fiosid + '/logo/' + bitmapId.toString(),
            'PUT', this.newImage, 'application/octet-stream', this.newImage.type)
            .do((val) => { console.log('Stations affected: ' + val)})
            .subscribe((val) => {
                this.channel.intBitMapId = bitmapId;
            }, (error) => {
                console.log(error);
                this.errorMsg = "Failed to create new logo. " + error;
            }, () => {
                this.inputImg.nativeElement.value = "";
                this.submitting = false;
                if (!this.errorMsg) {
                    this.isSuccess = true;
                    this.channelchange.emit(fiosid);
                }
            })
    }

    onModalClose() {
        console.log('modal closing');
        this.showForm = false;
        this._channel = undefined;
        this.isSuccess = false;
        this.stations = undefined;
    }
}