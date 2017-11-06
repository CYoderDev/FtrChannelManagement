import { Component, OnInit, ViewChild } from '@angular/core';
import { ChannelService } from '../Service/channel.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ModalComponent } from 'ng2-bs3-modal/ng2-bs3-modal';
import { IChannel } from '../Models/channel';
import { Observable } from 'rxjs/Rx';

@Component({
    templateUrl: 'app/Components/channel.component.html'
})

export class ChannelComponent implements OnInit
{
    @ViewChild('modal') modal: ModalComponent;
    channels: IChannel[];
    channel: IChannel[];
    vho: string;
    region: string;
    channelFrm: FormGroup;
    indLoading: boolean = false;
    imgLoading: boolean = false;
    msg: string;
    logo: any

    constructor(private fb: FormBuilder, private _channelService: ChannelService) { }

    ngOnInit(): void {
        this.channelFrm = this.fb.group({
            Id: [''],
            CallSign: [''],
            StationName: [''],
            BitMapId:['10000']
        })
        this.vho = '1';
        this.LoadChannels();
    }

    LoadChannels(): void {
        this.indLoading = true;
        let channels = null;
        if (this.region != null && this.region.length > 0 && this.region.toLowerCase() != 'none')
            channels = this._channelService.getBy('api/channel/region/', this.region)
        else
            channels = this._channelService.getBy('api/channel/vho/', this.vho)

        channels.subscribe(chs => { this.channels = chs; this.indLoading = false; },
            error => this.msg = <any>error);
    }

    LoadImage(bitmapId: number) {
        this.imgLoading = true;
        return this._channelService.getLogo("api/channellogo/", bitmapId.toString()).subscribe(data => {
            this.createImageFromBlob(data);
            this.imgLoading = false;
        }, error => {
            this.imgLoading = false;
            console.log(error);
            this.msg = <any>error;
        });
    }

    private createImageFromBlob(image: Blob) {
        let reader = new FileReader();
        reader.addEventListener("load", () => {
            this.logo = reader.result;
        }, false);

        if (image) {
            reader.readAsDataURL(image);
        }
    }
}