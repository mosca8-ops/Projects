package it.unimib.turistafelice.model;

public class PlaceIdApiResponse {

    private Place result;
    private String status;


    public PlaceIdApiResponse(Place result, String status) {
        this.result = result;
        this.status = status;
    }

    public Place getResult() {
        return result;
    }

    public String getStatus() {
        return status;
    }

    public void setResult(Place result) {
        this.result = result;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    @Override
    public String toString() {
        return "PlaceIdApiResponse{" +
                "result=" + result +
                ", status='" + status + '\'' +
                '}';
    }
}
