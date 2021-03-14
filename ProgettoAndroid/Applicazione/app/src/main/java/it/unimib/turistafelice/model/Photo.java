package it.unimib.turistafelice.model;

public class Photo {
    private String height;
    private String photo_reference;
    private String width;

    public Photo(String height, String photo_reference, String width) {
        this.height = height;
        this.photo_reference = photo_reference;
        this.width = width;
    }

    public String getHeight() {
        return height;
    }

    public void setHeight(String height) {
        this.height = height;
    }

    public String getPhoto_reference() {
        return photo_reference;
    }

    public void setPhoto_reference(String photo_reference) {
        this.photo_reference = photo_reference;
    }

    public String getWidth() {
        return width;
    }

    public void setWidth(String width) {
        this.width = width;
    }

    @Override
    public String toString() {
        return "Photo{" +
                "height='" + height + '\'' +
                ", photo_reference='" + photo_reference + '\'' +
                ", width='" + width + '\'' +
                '}';
    }
}
