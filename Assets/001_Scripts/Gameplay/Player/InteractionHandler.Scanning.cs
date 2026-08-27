using AstraNope.Data.Messages;
using AstraNope.Contracts;
using AstraNope.WorldObjects.Items;
using UnityEngine;

using AstraNope.Localization;
namespace AstraNope.Gameplay.Player
{
    public partial class InteractionHandler
    {
        private bool TryUpdateScan()
        {
            bool hasScanner = TryGetScannerRange(out float scanRange);
            float detectionRange = hasScanner ? scanRange : Mathf.Max(maxDistance, scanTargetPromptDistance);
            if (!Physics.Raycast(_trs.position, _trs.forward, out RaycastHit hit, detectionRange, interactLayer))
            {
                ClearScanFocus();
                return false;
            }

            ScannableTarget target = hit.collider.GetComponentInParent<ScannableTarget>();
            if (!target || target.IsScanned)
            {
                ClearScanFocus();
                return false;
            }

            if (_scanTarget != target)
            {
                if (_scanTarget) _scanTarget.SetVisual(0f, false);
                _scanTarget = target;
                _scanElapsed = 0f;
                CacheScanLabels(target);
            }

            if (_lastHitTrs != null)
            {
                _highlighter.SetHighlight(_lastHitTrs.gameObject, false);
                _lastHitTrs = null;
                _current = null;
            }
            _hasResourceTarget = false;
            _resourceInteraction?.ClearFocus();
            ClearCreatureFocus();
            _hasScanTarget = true;
            float progress = Mathf.Clamp01(_scanElapsed / target.ScanTime);

            if (!hasScanner)
            {
                target.SetVisual(progress, false);
                _uiPublisher.Publish(new InteractionUIMessage(
                    true, _scanNeedScannerLabel, "", progress, true));
                return true;
            }

            if (_scanHeld)
            {
                if (_scanElapsed <= 0f) _itemHolder?.TryPerformPrimaryAction();
                _scanElapsed = Mathf.Min(target.ScanTime, _scanElapsed + Time.deltaTime);
                progress = _scanElapsed / target.ScanTime;
                target.SetVisual(progress, true);
                int percent = Mathf.RoundToInt(progress * 100f);
                if (percent != _scanProgressPercent)
                {
                    _scanProgressPercent = percent;
                    _scanProgressLabel = $"{_scanProgressPrefix}{percent}%";
                }
                _uiPublisher.Publish(new InteractionUIMessage(
                    true, _scanProgressLabel, "RMB", progress));

                if (_scanElapsed >= target.ScanTime)
                    CompleteScan(target);
            }
            else
            {
                target.SetVisual(progress, false);
                _uiPublisher.Publish(new InteractionUIMessage(
                    true, _scanPromptLabel, "RMB", progress));
            }

            return true;
        }

        private void CacheScanLabels(ScannableTarget target)
        {
            string displayName = target.DisplayName;
            _scanNeedScannerLabel = L10n.F("k_d5e3beda12", displayName);
            _scanPromptLabel = L10n.F("k_7cfbfc8fdd", displayName);
            _scanProgressPrefix = L10n.F("k_bbb2119a8b", displayName);
            _scanProgressLabel = null;
            _scanProgressPercent = -1;
        }

        private bool TryGetScannerRange(out float range)
        {
            range = 0f;
            if (!_itemHolder || _itemHolder.HeldItem == null ||
                !_itemHolder.HeldItem.TryGetFeature<AstraNope.Data.Items.IScannableItem>(out var scanner))
                return false;

            range = Mathf.Max(maxDistance, scanner.Range);
            return range > 0f;
        }

        private void HandleScanHoldChanged(bool held)
        {
            _scanHeld = held;
            if (!held && _scanTarget)
            {
                _scanTarget.SetVisual(0f, false);
            }
        }

        private void CompleteScan(ScannableTarget target)
        {
            target.MarkScanned();
            _scanRewards?.Grant(target);

            _scanTarget = null;
            _scanElapsed = 0f;
            _hasScanTarget = false;
            _uiPublisher.Publish(new InteractionUIMessage(false, "", "RMB"));
        }

        private void ClearScanFocus()
        {
            if (_scanTarget) _scanTarget.SetVisual(0f, false);
            bool hadFocus = _hasScanTarget;
            _hasScanTarget = false;
            if (hadFocus) _uiPublisher.Publish(new InteractionUIMessage(false, "", "RMB"));
        }

        private void CancelScan()
        {
            ClearScanFocus();
        }
    }
}