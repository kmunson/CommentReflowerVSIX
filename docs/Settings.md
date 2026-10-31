# Settings

Comment Reflower is completely configurable in terms of the file types it works
on, and the bullet points and break flow strings it recognises. To change the
settings, go to the **Tools** menu, select **Options**, and select
**Comment Reflower AI**. You can also type "Comment Reflower" in the
**Search Settings** box.

Changes are saved as you make them, and take effect the next time you reflow a
comment. You don't need to restart Visual Studio.

The settings are split across four pages:

- [General](GeneralSettings.md)
- [Blocks](CommentBlockSettings.md)
- [Bullets](BulletPointSettings.md)
- [Break Flow Strings](BreakFlowStringsSettings.md)

Each page has a help button next to its title that opens its page here.

## Tabs in settings

The settings can't contain a tab character, so type `\t` for a tab. For
instance, a **Line Start** of `///\t` matches `///` followed by a tab.

## Invalid settings

Some problems can only be found when the settings are used, for instance a
regular expression that isn't valid. If the settings have a problem like this,
running a reflow command shows a message that names the setting and the
problem, and nothing is reflowed.
