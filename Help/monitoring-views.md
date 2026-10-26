# Clear monitoring views and newest-first sorting

Keywords: Clear view newest on top oldest on top paging POCSAG FLEX repeat grouped monitoring

Monitoring tables provide Clear view and an Oldest on top / Newest on top selector. Settings are saved per receiver/table. Sorting uses full last-received timestamps, including grouped messages; a repeated page moves according to its latest reception. Existing selection is retained. When scrolled away, the current reading anchor is preserved rather than forcing the table back to incoming rows. Sorting/refresh waits while an editable row is being edited.

Clear view hides currently displayed records without deleting saved frequency lists, site catalogs, maps, aliases or recordings and without resetting the decoder. New records and existing records that receive a new observation appear again. The saved-catalog count can remain nonzero because the underlying catalog is retained. Reset message history is a separate explicit action that clears message/decoder history. Existing Clear aircraft/observations and other reset actions retain their distinct meanings.

Controls cover POCSAG/FLEX, APRS, P25 messages/catalog/activity, DMR, NXDN, aircraft, sensors/meters/bursts, talkgroups and trunk history. Dedicated Clear diagnostics view, session view and HD/TV monitoring-view actions clear their displayed logs while reception can continue. These view controls do not acknowledge or pass live decoder acceptance.
