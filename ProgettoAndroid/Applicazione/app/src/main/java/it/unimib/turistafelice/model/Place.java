package it.unimib.turistafelice.model;

import android.os.Parcel;
import android.os.Parcelable;

import java.util.ArrayList;
import java.util.List;

public class Place implements Parcelable {

    private String formatted_address;
    private String name;
    private String place_id;
    private List<Photo> photos;

    protected Place(Parcel in) {
        formatted_address = in.readString();
        name = in.readString();
        place_id = in.readString();
        if (in.readByte() == 0x01) {
            photos = new ArrayList<Photo>();
            in.readList(photos, Photo.class.getClassLoader());
        } else {
            photos = null;
        }
    }

    @Override
    public int describeContents() {
        return 0;
    }

    @Override
    public void writeToParcel(Parcel dest, int flags) {
        dest.writeString(formatted_address);
        dest.writeString(name);
        dest.writeString(place_id);
        if (photos == null) {
            dest.writeByte((byte) (0x00));
        } else {
            dest.writeByte((byte) (0x01));
            dest.writeList(photos);
        }
    }

    @SuppressWarnings("unused")
    public static final Parcelable.Creator<Place> CREATOR = new Parcelable.Creator<Place>() {
        @Override
        public Place createFromParcel(Parcel in) {
            return new Place(in);
        }

        @Override
        public Place[] newArray(int size) {
            return new Place[size];
        }
    };

    public String getFormatted_address() {
        return formatted_address;
    }

    public void setFormatted_address(String formatted_address) {
        this.formatted_address = formatted_address;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getPlace_id() {
        return place_id;
    }

    public void setPlace_id(String place_id) {
        this.place_id = place_id;
    }

    public List<Photo> getPhotos() {
        return photos;
    }

    public void setPhotos(List<Photo> photos) {
        this.photos = photos;
    }

    @Override
    public String toString() {
        return "Place{" +
                "formatted_address='" + formatted_address + '\'' +
                ", name='" + name + '\'' +
                ", place_id='" + place_id + '\'' +
                ", photos=" + photos +
                '}';
    }
}