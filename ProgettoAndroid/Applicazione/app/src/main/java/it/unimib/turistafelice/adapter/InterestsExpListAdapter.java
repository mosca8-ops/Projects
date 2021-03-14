package it.unimib.turistafelice.adapter;

import android.content.Context;

import android.content.SharedPreferences;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseExpandableListAdapter;
import android.widget.CheckBox;
import android.widget.TextView;

import java.util.List;
import java.util.Map;
import java.util.Set;

import it.unimib.turistafelice.R;
import it.unimib.turistafelice.utils.Constants;

public class InterestsExpListAdapter extends BaseExpandableListAdapter {

    private Map<String, List<String>> interestsPerCategory;
    private List<String> categories;
    private Context context;
    private Set<String> userInterestsSet;
    private String userId;
    private String TAG ="InterestsExpListAdapter";

    public InterestsExpListAdapter(Context context, Map<String, List<String>> interestsPerCategory,
                                   List<String> categories, String userId){
        super();
        this.context = context;
        this.interestsPerCategory = interestsPerCategory;
        this.categories = categories;
        this.userId = userId;
    }

    //ritorna il numero di gruppi genitori stiamo per creare
    @Override
    public int getGroupCount() {
        return categories.size() ;
    }

    //ritorna il numero di view figlio per ogni genitore
    @Override
    public int getChildrenCount(int groupPosition) {
        Log.d(TAG, "getChildrenCount: " + categories.get(groupPosition));
        return interestsPerCategory.get(categories.get(groupPosition)).size();
    }

    //ritorna il nome della casella genitore
    @Override
    public Object getGroup(int groupPosition) {
        return categories.get(groupPosition);
    }
    //ritorna il nome della casella figlio in base al genitore

    @Override
    public Object getChild(int groupPosition, int childPosition) {
        return interestsPerCategory.get(categories.get(groupPosition)).get(childPosition);
    }
    @Override
    public long getGroupId(int groupPosition) {
        return groupPosition;
    }

    @Override
    public long getChildId(int groupPosition, int childPosition) {
        return childPosition;
    }

    @Override
    public boolean hasStableIds() {
        return false;
    }

    @Override
    public View getGroupView(int groupPosition, boolean isExpanded, View convertView, ViewGroup parent) {

        String categories = (String) getGroup(groupPosition);
        if(convertView == null){
            LayoutInflater inflater = (LayoutInflater)
                    context.getSystemService(Context.LAYOUT_INFLATER_SERVICE);
            assert inflater != null;
            convertView = inflater.inflate(R.layout.group_items_interests, null);
        }

        TextView txtParentGroup = (TextView) convertView.findViewById(R.id.text_parent_group);
        txtParentGroup.setText(categories);

        return convertView;
    }

    @Override
    public View getChildView(int groupPosition, int childPosition, boolean isLastChild, View convertView, ViewGroup parent) {

        String interesse = (String) getChild(groupPosition, childPosition);

        if(convertView == null){
            LayoutInflater inflater = (LayoutInflater)
                    context.getSystemService(Context.LAYOUT_INFLATER_SERVICE);
            assert inflater != null;
            convertView = inflater.inflate(R.layout.child_items_interests, null);
        }

        TextView txtChildInterestName = (TextView) convertView.findViewById(R.id.text_child_item);
        txtChildInterestName.setText(interesse);
        CheckBox interestCheckBox = (CheckBox) convertView.findViewById(R.id.isInterestWantedCheckbox);

        SharedPreferences sharedPreferences = context.getSharedPreferences(userId, context.MODE_PRIVATE);
        userInterestsSet = sharedPreferences.getStringSet(Constants.ALL_USER_INTERESTS, null);

        if(userInterestsSet == null)
            interestCheckBox.setChecked(false);

        else {
            if (userInterestsSet.contains(interesse))
                interestCheckBox.setChecked(true);
            else
                interestCheckBox.setChecked(false);
        }

        return convertView;
    }

    @Override
    public boolean isChildSelectable(int groupPosition, int childPosition) {
        return true;
    }
}
