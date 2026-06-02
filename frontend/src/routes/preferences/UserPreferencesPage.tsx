import { useEffect, useState } from 'react';
import { getUserPreferences, updateUserPreferences } from '../../lib/api';
import type {
  SingleItemEnvelope,
  UserPreference,
  UpdateUserPreferenceRequest,
} from '../../lib/types';
import { LoadingSpinner } from '../../components/LoadingSpinner';
import { ErrorMessage } from '../../components/ErrorMessage';

type PageState = 'loading' | 'success' | 'error';

const DEFAULT_USER_ID = 1;

const LABELS = {
  pageTitle: 'User Preferences',
  themeSection: 'Display Theme',
  themeLight: 'Light',
  themeDark: 'Dark',
  notificationsSection: 'Notification Settings',
  emailNotification: 'Email Notifications',
  pushNotification: 'Push Notifications',
  saveButton: 'Save Preferences',
  savingButton: 'Saving...',
  successMessage: 'Preferences saved successfully.',
  errorTitle: 'Preferences Error',
  loadError: 'Failed to load preferences.',
};

/**
 * User preferences page for managing display theme and notification settings.
 */
export function UserPreferencesPage() {
  const [pageState, setPageState] = useState<PageState>('loading');
  const [errorText, setErrorText] = useState<string>('');
  const [successVisible, setSuccessVisible] = useState(false);
  const [saving, setSaving] = useState(false);

  const [themePreference, setThemePreference] = useState<string>('light');
  const [emailNotificationIndicator, setEmailNotificationIndicator] =
    useState<boolean>(true);
  const [pushNotificationIndicator, setPushNotificationIndicator] =
    useState<boolean>(true);
  const [metadata, setMetadata] =
    useState<SingleItemEnvelope<UserPreference>['metadata'] | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function fetchPreferences() {
      try {
        const response = await getUserPreferences(DEFAULT_USER_ID);

        if (isMounted) {
          const { item } = response;
          setThemePreference(item.themePreference);
          setEmailNotificationIndicator(item.emailNotificationIndicator);
          setPushNotificationIndicator(item.pushNotificationIndicator);
          setMetadata(response.metadata);
          setPageState('success');
        }
      } catch (err) {
        if (isMounted) {
          const errorMessage =
            err instanceof Error ? err.message : LABELS.loadError;
          setErrorText(errorMessage);
          setPageState('error');
        }
      }
    }

    fetchPreferences();

    return () => {
      isMounted = false;
    };
  }, []);

  async function handleSave() {
    setSaving(true);
    setSuccessVisible(false);

    const request: UpdateUserPreferenceRequest = {
      themePreference,
      emailNotificationIndicator,
      pushNotificationIndicator,
    };

    try {
      const response = await updateUserPreferences(DEFAULT_USER_ID, request);
      setMetadata(response.metadata);
      setSuccessVisible(true);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to save preferences.';
      setErrorText(errorMessage);
    } finally {
      setSaving(false);
    }
  }

  if (pageState === 'loading') {
    return <LoadingSpinner />;
  }

  if (pageState === 'error') {
    return (
      <ErrorMessage errorTitle={LABELS.errorTitle} errorDescription={errorText} />
    );
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-[var(--color-brand-text)]">
        {LABELS.pageTitle}
      </h2>

      {successVisible && (
        <div
          className="bg-green-100 border border-green-300 text-green-800 px-4 py-3 rounded-lg"
          role="status"
          aria-live="polite"
        >
          {LABELS.successMessage}
        </div>
      )}

      {errorText && pageState === 'success' && (
        <div
          className="bg-red-100 border border-red-300 text-red-800 px-4 py-3 rounded-lg"
          role="alert"
        >
          {errorText}
        </div>
      )}

      <div className="bg-white rounded-lg shadow-md p-6 space-y-6">
        {/* Theme Selection */}
        <fieldset>
          <legend className="text-lg font-semibold text-[var(--color-brand-text)] mb-3">
            {LABELS.themeSection}
          </legend>
          <div className="flex gap-6">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="themePreference"
                value="light"
                checked={themePreference === 'light'}
                onChange={() => setThemePreference('light')}
                className="w-4 h-4 text-[var(--color-brand-primary)]"
                aria-label={LABELS.themeLight}
              />
              <span className="text-[var(--color-brand-text)]">
                {LABELS.themeLight}
              </span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="themePreference"
                value="dark"
                checked={themePreference === 'dark'}
                onChange={() => setThemePreference('dark')}
                className="w-4 h-4 text-[var(--color-brand-primary)]"
                aria-label={LABELS.themeDark}
              />
              <span className="text-[var(--color-brand-text)]">
                {LABELS.themeDark}
              </span>
            </label>
          </div>
        </fieldset>

        {/* Notification Settings */}
        <fieldset>
          <legend className="text-lg font-semibold text-[var(--color-brand-text)] mb-3">
            {LABELS.notificationsSection}
          </legend>
          <div className="space-y-3">
            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={emailNotificationIndicator}
                onChange={(e) =>
                  setEmailNotificationIndicator(e.target.checked)
                }
                className="w-4 h-4 rounded text-[var(--color-brand-primary)]"
                aria-label={LABELS.emailNotification}
              />
              <span className="text-[var(--color-brand-text)]">
                {LABELS.emailNotification}
              </span>
            </label>
            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={pushNotificationIndicator}
                onChange={(e) =>
                  setPushNotificationIndicator(e.target.checked)
                }
                className="w-4 h-4 rounded text-[var(--color-brand-primary)]"
                aria-label={LABELS.pushNotification}
              />
              <span className="text-[var(--color-brand-text)]">
                {LABELS.pushNotification}
              </span>
            </label>
          </div>
        </fieldset>

        {/* Save Button */}
        <div className="pt-2">
          <button
            type="button"
            onClick={handleSave}
            disabled={saving}
            className="bg-[var(--color-brand-primary)] text-white px-6 py-2.5 rounded-lg font-medium hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            aria-busy={saving}
          >
            {saving ? LABELS.savingButton : LABELS.saveButton}
          </button>
        </div>
      </div>

      {metadata && (
        <div className="bg-gray-50 rounded-lg p-4">
          <h3 className="text-sm font-semibold text-gray-600 mb-2">
            Response Metadata
          </h3>
          <p className="text-sm text-gray-500">
            <span className="font-medium">Transaction ID:</span>{' '}
            {metadata.transactionId}
          </p>
          <p className="text-sm text-gray-500">
            <span className="font-medium">Timestamp:</span>{' '}
            {metadata.timestamp}
          </p>
        </div>
      )}
    </div>
  );
}
